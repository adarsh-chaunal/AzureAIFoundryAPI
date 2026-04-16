using Application.Dtos.ClientSummary;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryAPI.Controllers;

[ApiController]
[Route("api/client-summary-requests")]
public sealed class ClientSummaryRequestsController : ControllerBase
{
    private readonly IClientSummaryService _service;

    public ClientSummaryRequestsController(IClientSummaryService service)
    {
        _service = service;
    }

    [HttpGet("{requestId:guid}")]
    public async Task<ActionResult<ClientSummaryRequestStatusDto>> GetStatus(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        var status = await _service.GetRequestStatusAsync(requestId, cancellationToken).ConfigureAwait(false);
        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }
}

