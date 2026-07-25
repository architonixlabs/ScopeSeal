using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Tenancy.Services;

using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.Tenancy;

public static class TenancyServiceCollectionExtensions
{
    public static IServiceCollection AddTenancyModule(this IServiceCollection services)
    {
        services.AddSingleton<TenancyModule>();
        return services;
    }
}

public sealed class TenancyModule : ModuleMarker;
