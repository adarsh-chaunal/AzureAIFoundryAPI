using Application.Dtos.Ehr;
using Application.Interfaces;
using Domain.Cloud;
using Infrastructure.Cloud.Interfaces;
using Infrastructure.Data.Sql;

namespace Application.Services;

public sealed class EhrClinicalCopilotService : IEhrClinicalCopilotService
{
    private readonly IAzureOpenAIService _azureOpenAi;
    private readonly ISqlDataAccess _sql;

    public EhrClinicalCopilotService(IAzureOpenAIService azureOpenAi, ISqlDataAccess sql)
    {
        _azureOpenAi = azureOpenAi;
        _sql = sql;
    }

    public async Task<EncounterSummaryResponseDto> SummarizeEncounterNarrativeAsync(
        EncounterSummaryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ClinicalNarrative))
        {
            throw new ArgumentException("Clinical narrative is required.");
        }

        var completion = await _azureOpenAi.GetChatCompletionAsync(new ChatCompletionRequest
        {
            Prompt = request.ClinicalNarrative,
            SystemPrompt =
                "You are a clinical documentation assistant. Summarize the encounter narrative for a clinician: concise, accurate, and free of new diagnoses or treatment plans not present in the source text.",
            Temperature = 0.2f,
            MaxTokens = 900
        }).ConfigureAwait(false);

        return new EncounterSummaryResponseDto
        {
            Summary = completion.Content,
            TokensUsed = completion.TokensUsed,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<PatientEducationResponseDto> DraftPatientEducationAsync(
        PatientEducationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new ArgumentException("Topic is required.");
        }

        var readingLevel = string.IsNullOrWhiteSpace(request.ReadingLevel)
            ? "sixth grade"
            : request.ReadingLevel!;

        var completion = await _azureOpenAi.GetChatCompletionAsync(new ChatCompletionRequest
        {
            Prompt = request.Topic,
            SystemPrompt =
                $"You write plain-language patient education suitable for a general adult audience at about a {readingLevel} reading level. Avoid jargon, keep sentences short, and include a short disclaimer that this is educational information and not individualized medical advice.",
            Temperature = 0.4f,
            MaxTokens = 900
        }).ConfigureAwait(false);

        return new PatientEducationResponseDto
        {
            HandoutText = completion.Content,
            TokensUsed = completion.TokensUsed,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<EhrConversationSummaryRowDto>> ListRecentClinicalConversationsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        if (take < 1 || take > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be between 1 and 200.");
        }

        var rows = await _sql.QueryAsync<EhrConversationSummaryRowDto>(
                "EXEC dbo.usp_Clinical_ListRecentConversations @Take;",
                new { Take = take },
                cancellationToken)
            .ConfigureAwait(false);

        return rows;
    }
}
