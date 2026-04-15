using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryAPI.Controllers.Azure;

/// <summary>
/// Route group for Azure retrieval-augmented generation (for example Azure AI Search + embeddings).
/// Add concrete endpoints here as RAG features are implemented.
/// </summary>
[ApiController]
[Route("api/azure/rag")]
public sealed class AzureRagController : ControllerBase
{
    [HttpGet]
    public IActionResult GetIntegrationState()
    {
        return Ok(new { integration = "rag", state = "planned" });
    }
}
