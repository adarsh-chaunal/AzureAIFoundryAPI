namespace Application.Dtos;

public class SendMessageRequestDto
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
}
