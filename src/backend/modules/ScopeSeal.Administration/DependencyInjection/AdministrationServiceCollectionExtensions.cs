using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Administration.Configuration;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.Administration.DependencyInjection;

public static class AdministrationServiceCollectionExtensions
{
    public static IServiceCollection AddAdministrationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<AdministrationOptions>()
            .Bind(configuration.GetSection(AdministrationOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<AdministrationModule>();
        return services;
    }
}

public sealed class AdministrationModule : ModuleMarker;
