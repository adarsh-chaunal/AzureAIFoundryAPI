using Application.Dtos;

namespace Application.Interfaces;

public interface IConversationService
{
    Task<SendMessageResponseDto> SendMessageAsync(SendMessageRequestDto request, CancellationToken cancellationToken = default);

    Task<SendMessageResponseDto> SendMessageWithContextAsync(SendMessageRequestDto request, CancellationToken cancellationToken = default);

    Task<ChatResponseDto> GetStructuredResponseAsync(StructuredPromptRequestDto request, CancellationToken cancellationToken = default);

    Task<ConversationHistoryDto> GetConversationHistoryAsync(string conversationId, CancellationToken cancellationToken = default);
}