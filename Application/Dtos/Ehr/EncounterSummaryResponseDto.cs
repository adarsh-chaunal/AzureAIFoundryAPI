namespace Application.Dtos.Ehr;

public class EncounterSummaryResponseDto
{
    public string Summary { get; set; } = string.Empty;

    public int TokensUsed { get; set; }

    public DateTime Timestamp { get; set; }
}
