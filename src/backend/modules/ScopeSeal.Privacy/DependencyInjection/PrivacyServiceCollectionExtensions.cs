using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Privacy.Configuration;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.Privacy.DependencyInjection;

public static class PrivacyServiceCollectionExtensions
{
    public static IServiceCollection AddPrivacyModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<PrivacyOptions>()
            .Bind(configuration.GetSection(PrivacyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<PrivacyModule>();
        return services;
    }
}

public sealed class PrivacyModule : ModuleMarker;
