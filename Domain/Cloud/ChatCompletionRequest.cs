namespace Domain.Cloud;

public class ChatCompletionRequest
{
    public string Prompt { get; set; } = string.Empty;

    public string? SystemPrompt { get; set; }

    public float Temperature { get; set; } = 0.7f;

    public int MaxTokens { get; set; } = 800;

    public List<ChatMessage>? ConversationHistory { get; set; }
}