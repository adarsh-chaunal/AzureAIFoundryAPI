using Application.Dtos.Ehr;

namespace Application.Interfaces;

public interface IEhrClinicalCopilotService
{
    Task<EncounterSummaryResponseDto> SummarizeEncounterNarrativeAsync(
        EncounterSummaryRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PatientEducationResponseDto> DraftPatientEducationAsync(
        PatientEducationRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EhrConversationSummaryRowDto>> ListRecentClinicalConversationsAsync(
        int take,
        CancellationToken cancellationToken = default);
}
