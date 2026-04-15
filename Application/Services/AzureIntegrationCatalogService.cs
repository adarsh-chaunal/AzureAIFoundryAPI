using Application.Dtos;
using Application.Interfaces;

namespace Application.Services;

public sealed class AzureIntegrationCatalogService : IAzureIntegrationCatalogService
{
    public Task<IReadOnlyList<AzureIntegrationEndpointDto>> GetIntegrationDirectoryAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AzureIntegrationEndpointDto> items =
        [
            new AzureIntegrationEndpointDto
            {
                Integration = "azure-root",
                HttpMethod = "GET",
                Route = "/api/azure",
                Summary = "Machine-readable directory of Azure integration routes exposed by this API."
            },
            new AzureIntegrationEndpointDto
            {
                Integration = "openai",
                HttpMethod = "POST",
                Route = "/api/azure/openai/chat/send",
                Summary = "Sends a chat message via Azure OpenAI and persists the exchange."
            },
            new AzureIntegrationEndpointDto
            {
                Integration = "openai",
                HttpMethod = "POST",
                Route = "/api/azure/openai/chat/send-with-context",
                Summary = "Sends a chat message with recent persisted messages supplied as model context."
            },
            new AzureIntegrationEndpointDto
            {
                Integration = "openai",
                HttpMethod = "POST",
                Route = "/api/azure/openai/chat/structured",
                Summary = "Asks the model for a sectioned response format (structured completion)."
            },
            new AzureIntegrationEndpointDto
            {
                Integration = "openai",
                HttpMethod = "GET",
                Route = "/api/azure/openai/chat/history/{conversationId}",
                Summary = "Returns persisted messages for a conversation identifier."
            },
            new AzureIntegrationEndpointDto
            {
                Integration = "rag",
                HttpMethod = "GET",
                Route = "/api/azure/rag",
                Summary = "Reserved route group for retrieval-augmented generation (Azure AI Search and related patterns)."
            },
            new AzureIntegrationEndpointDto
            {
                Integration = "audio",
                HttpMethod = "GET",
                Route = "/api/azure/audio",
                Summary = "Reserved route group for speech and audio processing (for example Azure Speech / batch transcription)."
            },
            new AzureIntegrationEndpointDto
            {
                Integration = "video",
                HttpMethod = "GET",
                Route = "/api/azure/video",
                Summary = "Reserved route group for video indexing and media workflows (for example Azure AI Video Indexer)."
            },
            new AzureIntegrationEndpointDto
            {
                Integration = "ehr-clinical-copilot",
                HttpMethod = "POST",
                Route = "/api/ehr/clinical-copilot/encounter-summary",
                Summary = "Summarizes clinician-authored encounter narrative text using Azure OpenAI."
            },
            new AzureIntegrationEndpointDto
            {
                Integration = "ehr-clinical-copilot",
                HttpMethod = "POST",
                Route = "/api/ehr/clinical-copilot/patient-education",
                Summary = "Drafts patient-facing education copy at a requested reading level."
            },
            new AzureIntegrationEndpointDto
            {
                Integration = "ehr-clinical-copilot",
                HttpMethod = "GET",
                Route = "/api/ehr/clinical-copilot/conversations/recent",
                Summary = "Lists recent persisted clinical conversations from SQL Server via stored procedure."
            }
        ];

        return Task.FromResult(items);
    }
}
