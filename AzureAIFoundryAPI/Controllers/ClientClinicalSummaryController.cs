using Application.Dtos.ClientSummary;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryAPI.Controllers;

[ApiController]
[Route("api/clients")]
public sealed class ClientClinicalSummaryController : ControllerBase
{
    private readonly IClientSummaryService _service;

    public ClientClinicalSummaryController(IClientSummaryService service)
    {
        _service = service;
    }

    [HttpPost("{clientId:int}/summaries")]
    public async Task<ActionResult<CreateClientSummaryResponseDto>> QueueClientSummary(
        [FromRoute] int clientId,
        CancellationToken cancellationToken)
    {
        var result = await _service.QueueClientSummaryAsync(clientId, cancellationToken).ConfigureAwait(false);
        return Accepted(result);
    }

    [HttpGet("{clientId:int}/summaries")]
    public async Task<ActionResult<IReadOnlyList<ClientSummaryRowDto>>> ListClientSummaries(
        [FromRoute] int clientId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var rows = await _service.ListClientSummariesAsync(clientId, take, cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }
}

