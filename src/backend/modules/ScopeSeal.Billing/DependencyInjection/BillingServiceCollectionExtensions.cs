using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Billing.Configuration;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.Billing.DependencyInjection;

public static class BillingServiceCollectionExtensions
{
    public static IServiceCollection AddBillingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<BillingOptions>()
            .Bind(configuration.GetSection(BillingOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.Mode != "Razorpay" || options.TestModeOnly == IsTestKey(options.Razorpay.KeyId),
                "Razorpay live keys are not permitted. Use test mode keys only.")
            .ValidateOnStart();

        services.AddSingleton<BillingModule>();
        return services;
    }

    private static bool IsTestKey(string keyId) =>
        keyId.StartsWith("rzp_test_", StringComparison.OrdinalIgnoreCase);
}

public sealed class BillingModule : ModuleMarker;
