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
        // Register application services
        services.AddScoped<IConversationService, ConversationService>();

        // Register AutoMapper manually
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}