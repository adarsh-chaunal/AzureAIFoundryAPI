using Application.Dtos;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryAPI.Controllers.Azure;

[ApiController]
[Route("api/azure/openai")]
public sealed class OpenAiController : ControllerBase
{
    private readonly IConversationService _conversationService;

    public OpenAiController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    [HttpPost("chat/send")]
    public async Task<ActionResult<SendMessageResponseDto>> SendMessage(
        [FromBody] SendMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _conversationService.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPost("chat/send-with-context")]
    public async Task<ActionResult<SendMessageResponseDto>> SendMessageWithContext(
        [FromBody] SendMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _conversationService.SendMessageWithContextAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }

    [HttpPost("chat/structured")]
    public async Task<ActionResult<ChatResponseDto>> GetStructuredResponse(
        [FromBody] StructuredPromptRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _conversationService.GetStructuredResponseAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("chat/history/{conversationId}")]
    public async Task<ActionResult<ConversationHistoryDto>> GetHistory(
        string conversationId,
        CancellationToken cancellationToken)
    {
        var history = await _conversationService.GetConversationHistoryAsync(conversationId, cancellationToken)
            .ConfigureAwait(false);
        return Ok(history);
    }
}
