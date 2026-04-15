namespace Application.Dtos.Ehr;

public class EhrConversationSummaryRowDto
{
    public string ConversationKey { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }
}
