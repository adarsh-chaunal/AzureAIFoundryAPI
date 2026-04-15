namespace Application.Dtos.Ehr;

public class PatientEducationResponseDto
{
    public string HandoutText { get; set; } = string.Empty;

    public int TokensUsed { get; set; }

    public DateTime Timestamp { get; set; }
}
