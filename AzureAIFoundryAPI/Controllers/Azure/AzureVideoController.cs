using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryAPI.Controllers.Azure;

/// <summary>
/// Route group for Azure video and media intelligence (for example Video Indexer or future media APIs).
/// </summary>
[ApiController]
[Route("api/azure/video")]
public sealed class AzureVideoController : ControllerBase
{
    [HttpGet]
    public IActionResult GetIntegrationState()
    {
        return Ok(new { integration = "video", state = "planned" });
    }
}
