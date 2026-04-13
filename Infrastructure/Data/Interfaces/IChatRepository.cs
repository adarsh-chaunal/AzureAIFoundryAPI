using Domain.Cloud;

namespace Infrastructure.Data.Interfaces;

public interface IChatRepository
{
    Task<ChatConversation> GetConversationAsync(string conversationId);

    Task SaveConversationAsync(ChatConversation conversation);

    Task<List<ChatMessage>> GetConversationHistoryAsync(string conversationId, int limit = 10);
}
