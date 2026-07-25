using ScopeSeal.Entitlements.Domain;

namespace ScopeSeal.Entitlements.Services;

public sealed record CapabilityCheckResult
{
    public required bool IsAllowed { get; init; }

    public Capability Capability { get; init; }

    public string? DenialReason { get; init; }

    public static CapabilityCheckResult Allowed(Capability capability) => new()
    {
        IsAllowed = true,
        Capability = capability
    };

    public static CapabilityCheckResult Denied(Capability capability, string reason) => new()
    {
        IsAllowed = false,
        Capability = capability,
        DenialReason = reason
    };
}

public sealed record UsageCheckResult
{
    public required bool IsAllowed { get; init; }

    public UsageMetric Metric { get; init; }

    public long CurrentUsage { get; init; }

    public long Limit { get; init; }

    public string? DenialReason { get; init; }

    public static UsageCheckResult Allowed(UsageMetric metric, long currentUsage, long limit) => new()
    {
        IsAllowed = true,
        Metric = metric,
        CurrentUsage = currentUsage,
        Limit = limit
    };

    public static UsageCheckResult Denied(UsageMetric metric, long currentUsage, long limit, string reason) => new()
    {
        IsAllowed = false,
        Metric = metric,
        CurrentUsage = currentUsage,
        Limit = limit,
        DenialReason = reason
    };
}

public sealed record EntitlementSummary
{
    public required Guid TenantId { get; init; }

    public required PlanCode PlanCode { get; init; }

    public required int PlanVersion { get; init; }

    public required EntitlementSource Source { get; init; }

    public required IReadOnlyCollection<Capability> Capabilities { get; init; }

    public required IReadOnlyDictionary<UsageMetric, UsageSummary> Usage { get; init; }
}

public sealed record UsageSummary
{
    public required long Current { get; init; }

    public required long Limit { get; init; }
}
