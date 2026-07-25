using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.Approvals.DependencyInjection;

public static class ApprovalsServiceCollectionExtensions
{
    public static IServiceCollection AddApprovalsModule(this IServiceCollection services)
    {
        services.AddSingleton<ApprovalsModule>();
        return services;
    }
}

public sealed class ApprovalsModule : ModuleMarker;
