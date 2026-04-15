using Infrastructure.Cloud.Interfaces;
using Infrastructure.Cloud.Services;
using Infrastructure.Data.Interfaces;
using Infrastructure.Data.Services;
using Infrastructure.Data.Sql;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class AddInfrastructureConfiguration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<ISqlDataAccess, SqlDataAccess>();

        services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();

        services.AddScoped<IChatRepository, SqlChatRepository>();

        return services;
    }
}