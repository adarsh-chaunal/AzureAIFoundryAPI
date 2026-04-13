using Application.Dtos;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IConversationService _conversationService;

    public ChatController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    [HttpPost("send")]
    public async Task<ActionResult<SendMessageResponseDto>> SendMessage(
        [FromBody] SendMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _conversationService.SendMessageAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("send-with-context")]
    public async Task<ActionResult<SendMessageResponseDto>> SendMessageWithContext(
        [FromBody] SendMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _conversationService.SendMessageWithContextAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("structured")]
    public async Task<ActionResult<ChatResponseDto>> GetStructuredResponse(
        [FromBody] string prompt,
        CancellationToken cancellationToken)
    {
        var response = await _conversationService.GetStructuredResponseAsync(prompt, cancellationToken);
        return Ok(response);
    }

    [HttpGet("history/{conversationId}")]
    public async Task<ActionResult<ConversationHistoryDto>> GetHistory(
        string conversationId,
        CancellationToken cancellationToken)
    {
        var history = await _conversationService.GetConversationHistoryAsync(conversationId, cancellationToken);
        return Ok(history);
    }
}