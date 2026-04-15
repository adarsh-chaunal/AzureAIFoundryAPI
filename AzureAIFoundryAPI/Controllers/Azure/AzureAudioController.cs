using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryAPI.Controllers.Azure;

/// <summary>
/// Route group for Azure speech and audio processing (for example Speech-to-Text, batch transcription, custom voice).
/// </summary>
[ApiController]
[Route("api/azure/audio")]
public sealed class AzureAudioController : ControllerBase
{
    [HttpGet]
    public IActionResult GetIntegrationState()
    {
        return Ok(new { integration = "audio", state = "planned" });
    }
}
