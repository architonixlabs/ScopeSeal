using ScopeSeal.Entitlements.Domain;

namespace ScopeSeal.Entitlements.Services;

public interface IEntitlementService
{
    Task<EntitlementSummary?> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<CapabilityCheckResult> CheckCapabilityAsync(
        Guid tenantId,
        Capability capability,
        CancellationToken cancellationToken = default);

    Task<UsageCheckResult> CheckUsageAsync(
        Guid tenantId,
        UsageMetric metric,
        long increment = 1,
        CancellationToken cancellationToken = default);

    Task RecordUsageAsync(
        Guid tenantId,
        UsageMetric metric,
        long increment = 1,
        CancellationToken cancellationToken = default);

    Task AssignDefaultFreePlanAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task AssignPlanAsync(
        Guid tenantId,
        PlanCode planCode,
        EntitlementSource source,
        CancellationToken cancellationToken = default);
}
