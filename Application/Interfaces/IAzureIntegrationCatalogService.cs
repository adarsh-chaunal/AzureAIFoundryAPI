using Application.Dtos;

namespace Application.Interfaces;

public interface IAzureIntegrationCatalogService
{
    Task<IReadOnlyList<AzureIntegrationEndpointDto>> GetIntegrationDirectoryAsync(
        CancellationToken cancellationToken = default);
}
