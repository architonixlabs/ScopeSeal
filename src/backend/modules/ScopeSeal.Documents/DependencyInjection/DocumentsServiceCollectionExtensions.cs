using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.Documents.DependencyInjection;

public static class DocumentsServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentsModule(this IServiceCollection services)
    {
        services.AddSingleton<DocumentsModule>();
        return services;
    }
}

public sealed class DocumentsModule : ModuleMarker;
