namespace Application.Dtos;

public class ConversationHistoryDto
{
    public string ConversationId { get; set; } = string.Empty;
    public List<ChatMessageDto> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}