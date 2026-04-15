namespace Application.Dtos.Ehr;

public class PatientEducationRequestDto
{
    public string Topic { get; set; } = string.Empty;

    public string? ReadingLevel { get; set; }
}
