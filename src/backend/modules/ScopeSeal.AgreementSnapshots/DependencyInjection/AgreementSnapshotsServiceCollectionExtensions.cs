using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.AgreementSnapshots.DependencyInjection;

public static class AgreementSnapshotsServiceCollectionExtensions
{
    public static IServiceCollection AddAgreementSnapshotsModule(this IServiceCollection services)
    {
        services.AddSingleton<AgreementSnapshotsModule>();
        return services;
    }
}

public sealed class AgreementSnapshotsModule : ModuleMarker;
