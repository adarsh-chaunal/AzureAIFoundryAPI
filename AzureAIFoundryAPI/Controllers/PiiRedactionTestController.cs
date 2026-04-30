using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using AzureAIFoundryAPI.Services;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AzureAIFoundryAPI.Controllers;

[ApiController]
[Route("api/test")]
public sealed class PiiRedactionTestController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly PhiScrubber _scrubber;

    public PiiRedactionTestController(IConfiguration configuration, PhiScrubber scrubber)
    {
        _configuration = configuration;
        _scrubber = scrubber;
    }

    [HttpGet("clients/{clientId:int}/unsanitized-prompt")]
    public async Task<ActionResult<UnsanitizedPromptResponse>> GetUnsanitizedClinicalPrompt(
        [FromRoute] int clientId,
        CancellationToken cancellationToken)
    {
        if (clientId <= 0)
        {
            return BadRequest(new { error = "ClientId must be a positive integer." });
        }

        var prompt = await LoadClientClinicalBundleAsTextAsync(clientId, cancellationToken)
            .ConfigureAwait(false);

        return Ok(new UnsanitizedPromptResponse(
            clientId,
            prompt,
            prompt.Length,
            "Contains unsanitized clinical text. Use only for local/debug testing."));
    }

    [HttpPost("pii-redaction")]
    public async Task<ActionResult<RedactionTestResponse>> TestConfiguredRedaction(
        [FromBody] RedactionTestRequest request,
        CancellationToken cancellationToken)
    {
        return await RedactRequestTextAsync(request, provider: null, cancellationToken)
            .ConfigureAwait(false);
    }

    [HttpPost("pii-redaction/{provider}")]
    public async Task<ActionResult<RedactionTestResponse>> TestRedactionAlgorithm(
        [FromRoute] string provider,
        [FromBody] RedactionTestRequest request,
        CancellationToken cancellationToken)
    {
        return await RedactRequestTextAsync(request, provider, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ActionResult<RedactionTestResponse>> RedactRequestTextAsync(
        RedactionTestRequest request,
        string? provider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Provide text to redact in the 'text' field." });
        }

        try
        {
            var result = await _scrubber.ScrubAsync(request.Text, provider, cancellationToken)
                .ConfigureAwait(false);

            return Ok(new RedactionTestResponse(result.Text, result.Provider, result.UsedFallback, result.Error));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new { error = ex.Message });
        }
    }

    private async Task<string> LoadClientClinicalBundleAsTextAsync(int clientId, CancellationToken cancellationToken)
    {
        var sourceConnectionString =
            _configuration.GetConnectionString("EhrClinicalSource")
            ?? _configuration["ConnectionStrings:EhrClinicalSource"]
            ?? _configuration.GetConnectionString("EhrClinical")
            ?? _configuration["ConnectionStrings:EhrClinical"]
            ?? _configuration["Sql:SourceConnectionString"]
            ?? throw new InvalidOperationException(
                "Missing source/EHR DB connection string. Set ConnectionStrings:EhrClinicalSource or ConnectionStrings:EhrClinical.");

        await using var connection = new SqlConnection(sourceConnectionString);
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

    public sealed record RedactionTestRequest(
        [property: JsonPropertyName("text")] string? Text);

    public sealed record RedactionTestResponse(
        [property: JsonPropertyName("redactedText")] string RedactedText,
        [property: JsonPropertyName("provider")] string Provider,
        [property: JsonPropertyName("usedFallback")] bool UsedFallback,
        [property: JsonPropertyName("error")] string? Error);

    public sealed record UnsanitizedPromptResponse(
        [property: JsonPropertyName("clientId")] int ClientId,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("length")] int Length,
        [property: JsonPropertyName("warning")] string Warning);
}
