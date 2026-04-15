namespace Application.Dtos;

public class AzureIntegrationEndpointDto
{
    public string Integration { get; set; } = string.Empty;

    public string HttpMethod { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;
}
