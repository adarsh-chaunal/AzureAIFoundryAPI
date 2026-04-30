using System.Text;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Infrastructure.Cloud.Interfaces;
using Domain.Cloud;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace EhrClinical.AzureFunction
{
    public sealed class Function1
    {
        private readonly ILogger _logger;
        private readonly IAzureOpenAIService _openAi;
        private readonly PhiScrubber _scrubber;
        private readonly string _sourceConnectionString;
        private readonly string _aiConnectionString;

        public Function1(
            ILoggerFactory loggerFactory,
            IAzureOpenAIService openAi,
            PhiScrubber scrubber,
            IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
            _openAi = openAi;
            _scrubber = scrubber;

            _sourceConnectionString =
                configuration.GetConnectionString("EhrClinicalSource")
                ?? configuration["ConnectionStrings:EhrClinicalSource"]
                ?? configuration["Sql:SourceConnectionString"]
                ?? throw new InvalidOperationException(
                    "Missing Source/EHR DB connection string. Set ConnectionStrings:EhrClinicalSource (recommended) or Sql:SourceConnectionString.");

            _aiConnectionString =
                configuration.GetConnectionString("EhrClinicalAi")
                ?? configuration["ConnectionStrings:EhrClinicalAi"]
                ?? configuration.GetConnectionString("EhrClinical")
                ?? configuration["ConnectionStrings:EhrClinical"]
                ?? configuration["Sql:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Missing AI DB connection string. Set ConnectionStrings:EhrClinicalAi (recommended).");
        }

        private sealed record ClientSummaryQueueMessage(Guid RequestId, int ClientId);

        [Function("ClientClinicalSummaryProcessor")]
        public async Task Run(
            [ServiceBusTrigger("%ClientSummaryQueueName%", Connection = "ServiceBusConnection")] string messageBody,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("ClientClinicalSummaryProcessor received message.");

            ClientSummaryQueueMessage msg;
            try
            {
                msg = JsonSerializer.Deserialize<ClientSummaryQueueMessage>(messageBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("Queue message was null.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid queue message body.");
                throw; // let SB retry / DLQ
            }

            // Mark request as Processing (best-effort; table/proc names should align with your DB)
            await TryUpdateRequestStatusAsync(msg.RequestId, "Processing", null, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                // 1) Load clinical data bundle for the client
                var bundleText = await LoadClientClinicalBundleAsTextAsync(msg.ClientId, cancellationToken)
                    .ConfigureAwait(false);

                // 2) Redact PHI before anything is sent to Azure OpenAI.
                var scrubbed = await _scrubber.ScrubAsync(bundleText, clientNeutralName: "Client", cancellationToken)
                    .ConfigureAwait(false);
                _logger.LogInformation(
                    "PHI redaction completed with Provider={Provider}, UsedFallback={UsedFallback}",
                    scrubbed.Provider,
                    scrubbed.UsedFallback);

                // 3) Generate summary using Azure OpenAI
                var completion = await _openAi.GetChatCompletionAsync(new ChatCompletionRequest
                {
                    Prompt = scrubbed.Text,
                    SystemPrompt =
                        "You are a clinical documentation assistant. Produce a concise but clinically faithful longitudinal client summary from the provided clinical record excerpts. " +
                        "Do not invent diagnoses, medications, or plans not present in the source. If information is missing, state it as unknown. " +
                        "Output sections: Presenting concerns, Relevant history, Symptoms/functional status, Risk/safety, Interventions/treatment, Progress/response, Goals, Plan/next steps.",
                    Temperature = 0.2f,
                    MaxTokens = 1200
                }).ConfigureAwait(false);

                // 4) Persist summary and mark request Completed
                var summaryId = await SaveClientSummaryAsync(msg.RequestId, msg.ClientId, completion, cancellationToken)
                    .ConfigureAwait(false);

                await TryUpdateRequestStatusAsync(msg.RequestId, "Completed", null, cancellationToken, summaryId)
                    .ConfigureAwait(false);

                _logger.LogInformation("ClientClinicalSummaryProcessor completed RequestId={RequestId}", msg.RequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClientClinicalSummaryProcessor failed RequestId={RequestId}", msg.RequestId);
                await TryUpdateRequestStatusAsync(msg.RequestId, "Failed", ex.Message, cancellationToken)
                    .ConfigureAwait(false);
                throw; // allow retry; idempotency should be enforced in DB if needed
            }
        }

        [Function("TestPresidioRedaction")]
        public async Task<HttpResponseData> TestPresidioRedaction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "test/pii-redaction")] HttpRequestData request,
            CancellationToken cancellationToken)
        {
            return await RedactRequestTextAsync(request, provider: null, cancellationToken)
                .ConfigureAwait(false);
        }

        [Function("TestPiiRedactionAlgorithm")]
        public async Task<HttpResponseData> TestPiiRedactionAlgorithm(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "test/pii-redaction/{provider}")] HttpRequestData request,
            string provider,
            CancellationToken cancellationToken)
        {
            return await RedactRequestTextAsync(request, provider, cancellationToken)
                .ConfigureAwait(false);
        }

        [Function("GetUnsanitizedClinicalPrompt")]
        public async Task<HttpResponseData> GetUnsanitizedClinicalPrompt(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "test/clients/{clientId:int}/unsanitized-prompt")] HttpRequestData request,
            int clientId,
            CancellationToken cancellationToken)
        {
            if (clientId <= 0)
            {
                var badResponse = request.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "ClientId must be a positive integer." }, cancellationToken)
                    .ConfigureAwait(false);
                return badResponse;
            }

            var prompt = await LoadClientClinicalBundleAsTextAsync(clientId, cancellationToken)
                .ConfigureAwait(false);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(
                    new UnsanitizedPromptResponse(
                        clientId,
                        prompt,
                        prompt.Length,
                        "Contains unsanitized clinical text. Use only for local/debug testing."),
                    cancellationToken)
                .ConfigureAwait(false);
            return response;
        }

        private async Task<HttpResponseData> RedactRequestTextAsync(
            HttpRequestData request,
            string? provider,
            CancellationToken cancellationToken)
        {
            RedactionTestRequest? payload;
            try
            {
                payload = await JsonSerializer.DeserializeAsync<RedactionTestRequest>(
                        request.Body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (JsonException)
            {
                var invalidResponse = request.CreateResponse(HttpStatusCode.BadRequest);
                await invalidResponse.WriteAsJsonAsync(new { error = "Request body must be valid JSON." }, cancellationToken)
                    .ConfigureAwait(false);
                return invalidResponse;
            }

            if (string.IsNullOrWhiteSpace(payload?.Text))
            {
                var badResponse = request.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Provide text to redact in the 'text' field." }, cancellationToken)
                    .ConfigureAwait(false);
                return badResponse;
            }

            PhiScrubResult result;
            try
            {
                result = await _scrubber.ScrubAsync(payload.Text, "Client", provider, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                var badProviderResponse = request.CreateResponse(HttpStatusCode.BadRequest);
                await badProviderResponse.WriteAsJsonAsync(new { error = ex.Message }, cancellationToken)
                    .ConfigureAwait(false);
                return badProviderResponse;
            }

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(
                    new RedactionTestResponse(result.Text, result.Provider, result.UsedFallback, result.Error),
                    cancellationToken)
                .ConfigureAwait(false);
            return response;
        }

        private async Task<string> LoadClientClinicalBundleAsTextAsync(int clientId, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(_sourceConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var rows = (await connection.QueryAsync<ClinicalRow>(
                    new CommandDefinition(
                        "EXEC dbo.usp_ClientClinical_GetBundle @ClientId;",
                        new { ClientId = clientId },
                        cancellationToken: cancellationToken)))
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("CLIENT CLINICAL RECORD EXCERPTS");
            sb.AppendLine($"ClientId: {clientId}");
            sb.AppendLine();

            foreach (var r in rows)
            {
                if (!string.IsNullOrWhiteSpace(r.Section))
                {
                    sb.AppendLine($"## {r.Section}");
                }

                if (!string.IsNullOrWhiteSpace(r.EncounterDate))
                {
                    sb.AppendLine($"Date: {r.EncounterDate}");
                }

                if (!string.IsNullOrWhiteSpace(r.Content))
                {
                    sb.AppendLine(r.Content);
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private sealed class ClinicalRow
        {
            public string? Section { get; set; }
            public string? EncounterDate { get; set; }
            public string? Content { get; set; }
        }

        private sealed record RedactionTestRequest(
            [property: JsonPropertyName("text")] string? Text);

        private sealed record RedactionTestResponse(
            [property: JsonPropertyName("redactedText")] string RedactedText,
            [property: JsonPropertyName("provider")] string Provider,
            [property: JsonPropertyName("usedFallback")] bool UsedFallback,
            [property: JsonPropertyName("error")] string? Error);

        private sealed record UnsanitizedPromptResponse(
            [property: JsonPropertyName("clientId")] int ClientId,
            [property: JsonPropertyName("prompt")] string Prompt,
            [property: JsonPropertyName("length")] int Length,
            [property: JsonPropertyName("warning")] string Warning);

        private async Task<Guid> SaveClientSummaryAsync(
            Guid requestId,
            int clientId,
            ChatCompletionResponse completion,
            CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(_aiConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var summaryId = await connection.QuerySingleOrDefaultAsync<Guid>(
                    new CommandDefinition(
                        "EXEC dbo.usp_AIClientSummary_Insert @RequestId, @ClientId, @SummaryText, @TokensUsed, @Model;",
                        new
                        {
                            RequestId = requestId,
                            ClientId = clientId,
                            SummaryText = completion.Content,
                            TokensUsed = completion.TokensUsed,
                            Model = completion.Model
                        },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return summaryId == Guid.Empty ? Guid.NewGuid() : summaryId;
        }

        private async Task TryUpdateRequestStatusAsync(
            Guid requestId,
            string status,
            string? error,
            CancellationToken cancellationToken,
            Guid? latestSummaryId = null)
        {
            await using var connection = new SqlConnection(_aiConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await connection.ExecuteAsync(
                    new CommandDefinition(
                        "EXEC dbo.usp_AIClientSummaryRequest_UpdateStatus @RequestId, @Status, @ErrorMessage, @LatestSummaryId;",
                        new
                        {
                            RequestId = requestId,
                            Status = status,
                            ErrorMessage = error,
                            LatestSummaryId = latestSummaryId
                        },
                        cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }
    }
}
