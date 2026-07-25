using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.Audit.DependencyInjection;

public static class AuditServiceCollectionExtensions
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        services.AddSingleton<AuditModule>();
        return services;
    }
}

public sealed class AuditModule : ModuleMarker;
