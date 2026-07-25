using System.ComponentModel.DataAnnotations;

namespace ScopeSeal.Billing.Configuration;

public sealed class BillingOptions
{
    public const string SectionName = "ScopeSeal:Billing";

    [Required]
    [RegularExpression("^(Disabled|LocalTest|Razorpay)$")]
    public string Mode { get; init; } = "Disabled";

    public bool TestModeOnly { get; init; } = true;

    [Range(1, 30)]
    public int FailedPaymentGracePeriodDays { get; init; } = 7;

    [Required]
    public RazorpayOptions Razorpay { get; init; } = new();

    [Required]
    public BillingPlanMappingOptions Plans { get; init; } = new();
}

public sealed class RazorpayOptions
{
    public string KeyId { get; init; } = string.Empty;

    public string KeySecret { get; init; } = string.Empty;

    public string WebhookSecret { get; init; } = string.Empty;

    public string? WebhookSecretPrevious { get; init; }
}

public sealed class BillingPlanMappingOptions
{
    public PlanIntervalMapping Pro { get; init; } = new();

    public PlanIntervalMapping Business { get; init; } = new();
}

public sealed class PlanIntervalMapping
{
    public string MonthlyRazorpayPlanId { get; init; } = string.Empty;

    public string AnnualRazorpayPlanId { get; init; } = string.Empty;
}
