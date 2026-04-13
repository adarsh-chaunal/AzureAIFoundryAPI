namespace Application.Dtos;

public class SendMessageResponseDto
{
    public string Response { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}