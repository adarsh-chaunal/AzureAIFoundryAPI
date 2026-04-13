namespace Application.Dtos;

public class ChatResponseDto
{
    public string Text { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public DateTime Timestamp { get; set; }
}
