using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Shared.DependencyInjection;

public static class SharedServiceCollectionExtensions
{
    public static IServiceCollection AddScopeSealShared(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ScopeSealOptions>()
            .Bind(configuration.GetSection(ScopeSealOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
