namespace Domain.Cloud;

public class ChatCompletionResponse
{
    public string Content { get; set; } = string.Empty;

    public int TokensUsed { get; set; }

    public string Model { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }
}