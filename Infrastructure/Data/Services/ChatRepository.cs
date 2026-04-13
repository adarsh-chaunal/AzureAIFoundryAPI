using Domain.Cloud;
using Infrastructure.Data.Interfaces;

namespace Infrastructure.Data.Services;

public class ChatRepository : IChatRepository
{
    // In-memory storage for demo (replace with actual database)
    private static readonly Dictionary<string, ChatConversation> _conversations = new();
    private static readonly object _lock = new();

    public Task<ChatConversation> GetConversationAsync(string conversationId)
    {
        lock (_lock)
        {
            _conversations.TryGetValue(conversationId, out var conversation);
            return Task.FromResult(conversation ?? new ChatConversation { Id = conversationId });
        }
    }

    public Task SaveConversationAsync(ChatConversation conversation)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(conversation.Id))
            {
                conversation.Id = Guid.NewGuid().ToString();
                conversation.CreatedAt = DateTime.UtcNow;
            }

            conversation.UpdatedAt = DateTime.UtcNow;
            _conversations[conversation.Id] = conversation;
        }

        return Task.CompletedTask;
    }

    public Task<List<ChatMessage>> GetConversationHistoryAsync(string conversationId, int limit = 10)
    {
        lock (_lock)
        {
            if (_conversations.TryGetValue(conversationId, out var conversation))
            {
                var history = conversation.Messages
                    .OrderByDescending(m => m.Timestamp)
                    .Take(limit)
                    .OrderBy(m => m.Timestamp)
                    .ToList();

                return Task.FromResult(history);
            }

            return Task.FromResult(new List<ChatMessage>());
        }
    }
}