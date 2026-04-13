using Infrastructure.Cloud.Interfaces;
using Infrastructure.Cloud.Services;
using Infrastructure.Data.Interfaces;
using Infrastructure.Data.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class AddInfrastructureConfiguration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register cloud services
        services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();

        // Register data services
        services.AddScoped<IChatRepository, ChatRepository>();

        return services;
    }
}