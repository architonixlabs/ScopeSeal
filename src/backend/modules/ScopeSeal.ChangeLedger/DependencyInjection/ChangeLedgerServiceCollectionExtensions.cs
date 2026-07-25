using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.ChangeLedger.DependencyInjection;

public static class ChangeLedgerServiceCollectionExtensions
{
    public static IServiceCollection AddChangeLedgerModule(this IServiceCollection services)
    {
        services.AddSingleton<ChangeLedgerModule>();
        return services;
    }
}

public sealed class ChangeLedgerModule : ModuleMarker;
