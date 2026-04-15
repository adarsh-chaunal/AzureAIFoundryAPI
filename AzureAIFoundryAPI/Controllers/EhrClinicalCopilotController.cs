using Application.Dtos.Ehr;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryAPI.Controllers;

[ApiController]
[Route("api/ehr/clinical-copilot")]
public sealed class EhrClinicalCopilotController : ControllerBase
{
    private readonly IEhrClinicalCopilotService _clinicalCopilotService;

    public EhrClinicalCopilotController(IEhrClinicalCopilotService clinicalCopilotService)
    {
        _clinicalCopilotService = clinicalCopilotService;
    }

    [HttpPost("encounter-summary")]
    public async Task<ActionResult<EncounterSummaryResponseDto>> SummarizeEncounter(
        [FromBody] EncounterSummaryRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _clinicalCopilotService.SummarizeEncounterNarrativeAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPost("patient-education")]
    public async Task<ActionResult<PatientEducationResponseDto>> DraftPatientEducation(
        [FromBody] PatientEducationRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _clinicalCopilotService.DraftPatientEducationAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("conversations/recent")]
    public async Task<ActionResult<IReadOnlyList<EhrConversationSummaryRowDto>>> ListRecentConversations(
        [FromQuery] int take = 25,
        CancellationToken cancellationToken = default)
    {
        var rows = await _clinicalCopilotService.ListRecentClinicalConversationsAsync(take, cancellationToken)
            .ConfigureAwait(false);
        return Ok(rows);
    }
}
