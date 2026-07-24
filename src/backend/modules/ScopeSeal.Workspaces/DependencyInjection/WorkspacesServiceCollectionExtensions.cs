using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.Workspaces.DependencyInjection;

public static class WorkspacesServiceCollectionExtensions
{
    public static IServiceCollection AddWorkspacesModule(this IServiceCollection services)
    {
        services.AddSingleton<WorkspacesModule>();
        return services;
    }
}

public sealed class WorkspacesModule : ModuleMarker;
