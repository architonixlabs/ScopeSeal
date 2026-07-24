using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ScopeSeal.Entitlements.Configuration;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.Entitlements.DependencyInjection;

public static class EntitlementsServiceCollectionExtensions
{
    public static IServiceCollection AddEntitlementsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<EntitlementsModule>();

        services
            .AddOptions<PlansOptions>()
            .Bind(configuration.GetSection(PlansOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}

public sealed class EntitlementsModule : ModuleMarker;
