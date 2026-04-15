using Application.Dtos;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryAPI.Controllers.Azure;

[ApiController]
[Route("api/azure")]
public sealed class AzureRootController : ControllerBase
{
    private readonly IAzureIntegrationCatalogService _catalog;

    public AzureRootController(IAzureIntegrationCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AzureIntegrationEndpointDto>>> GetIntegrationDirectory(
        CancellationToken cancellationToken)
    {
        var items = await _catalog.GetIntegrationDirectoryAsync(cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }
}
