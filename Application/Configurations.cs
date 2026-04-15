using Application.Interfaces;
using Application.Mappers;
using Application.Services;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IEhrClinicalCopilotService, EhrClinicalCopilotService>();
        services.AddScoped<IAzureIntegrationCatalogService, AzureIntegrationCatalogService>();

        // Register AutoMapper manually
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}