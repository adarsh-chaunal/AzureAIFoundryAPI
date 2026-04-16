namespace Application.Dtos.ClientSummary;

public sealed class CreateClientSummaryResponseDto
{
    public Guid RequestId { get; set; }
    public string Status { get; set; } = "Queued";
}

public sealed class ClientSummaryRequestStatusDto
{
    public Guid RequestId { get; set; }
    public int ClientId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Guid? LatestSummaryId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? LatestSummaryText { get; set; }
}

public sealed class ClientSummaryRowDto
{
    public Guid SummaryId { get; set; }
    public Guid RequestId { get; set; }
    public int ClientId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int? TokensUsed { get; set; }
    public string? Model { get; set; }
    public string SummaryText { get; set; } = string.Empty;
}

