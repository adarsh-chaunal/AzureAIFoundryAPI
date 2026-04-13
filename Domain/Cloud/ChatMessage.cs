namespace Domain.Cloud;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; }
}
