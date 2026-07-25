using ScopeSeal.Entitlements.Domain;

namespace ScopeSeal.Billing.Domain;

public sealed class TenantSubscription
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public PlanCode PlanCode { get; set; }

    public BillingInterval Interval { get; set; }

    public string ExternalSubscriptionId { get; set; } = string.Empty;

    public SubscriptionStatus Status { get; set; }

    public bool EntitlementGranted { get; set; }

    public DateTime? GracePeriodEndsAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
