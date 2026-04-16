using Application.Dtos.ClientSummary;

namespace Application.Interfaces;

public interface IClientSummaryService
{
    Task<CreateClientSummaryResponseDto> QueueClientSummaryAsync(int clientId, CancellationToken cancellationToken = default);
    Task<ClientSummaryRequestStatusDto?> GetRequestStatusAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientSummaryRowDto>> ListClientSummariesAsync(int clientId, int take = 50, CancellationToken cancellationToken = default);
}

