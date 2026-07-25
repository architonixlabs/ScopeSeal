using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.Extraction.DependencyInjection;

public static class ExtractionServiceCollectionExtensions
{
    public static IServiceCollection AddExtractionModule(this IServiceCollection services)
    {
        services.AddSingleton<ExtractionModule>();
        return services;
    }
}

public sealed class ExtractionModule : ModuleMarker;
