using Microsoft.EntityFrameworkCore;
using Npgsql;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Infrastructure.Persistence;

namespace ScopeSeal.Infrastructure.Services;

public sealed class EntitlementService(ApplicationDbContext dbContext) : IEntitlementService
{
    private static readonly Capability[] PrivacyCapabilities =
    [
        Capability.CanAccessPrivacyCentre,
        Capability.CanRequestDataExport,
        Capability.CanRequestAccountDeletion
    ];

    public async Task<EntitlementSummary?> GetSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await GetActiveAssignmentAsync(tenantId, cancellationToken);
        if (assignment is null)
        {
            return null;
        }

        var limits = PlanLimitsSnapshot.FromJson(assignment.PlanVersion.LimitsJson);
        var usage = await BuildUsageSummaryAsync(tenantId, limits, cancellationToken);

        return new EntitlementSummary
        {
            TenantId = tenantId,
            PlanCode = assignment.PlanVersion.PlanCode,
            PlanVersion = assignment.PlanVersion.Version,
            Source = assignment.Source,
            Capabilities = limits.EnabledCapabilities.OrderBy(c => c).ToArray(),
            Usage = usage
        };
    }

    public async Task<CapabilityCheckResult> CheckCapabilityAsync(
        Guid tenantId,
        Capability capability,
        CancellationToken cancellationToken = default)
    {
        if (PrivacyCapabilities.Contains(capability))
        {
            return CapabilityCheckResult.Allowed(capability);
        }

        var assignment = await GetActiveAssignmentAsync(tenantId, cancellationToken);
        if (assignment is null)
        {
            return CapabilityCheckResult.Denied(capability, "No active plan assignment.");
        }

        var limits = PlanLimitsSnapshot.FromJson(assignment.PlanVersion.LimitsJson);
        if (!limits.EnabledCapabilities.Contains(capability))
        {
            return CapabilityCheckResult.Denied(
                capability,
                $"Capability '{capability}' is not included in the {assignment.PlanVersion.PlanCode} plan.");
        }

        var usageMetric = MapCapabilityToUsageMetric(capability);
        if (usageMetric is null)
        {
            return CapabilityCheckResult.Allowed(capability);
        }

        var usageCheck = await CheckUsageAsync(tenantId, usageMetric.Value, 1, cancellationToken);
        return usageCheck.IsAllowed
            ? CapabilityCheckResult.Allowed(capability)
            : CapabilityCheckResult.Denied(capability, usageCheck.DenialReason ?? "Usage limit reached.");
    }

    public async Task<UsageCheckResult> CheckUsageAsync(
        Guid tenantId,
        UsageMetric metric,
        long increment = 1,
        CancellationToken cancellationToken = default)
    {
        var assignment = await GetActiveAssignmentAsync(tenantId, cancellationToken);
        if (assignment is null)
        {
            return UsageCheckResult.Denied(metric, 0, 0, "No active plan assignment.");
        }

        var limits = PlanLimitsSnapshot.FromJson(assignment.PlanVersion.LimitsJson);
        var limit = GetLimitForMetric(limits, metric);
        var current = await GetCurrentUsageAsync(tenantId, metric, cancellationToken);

        if (current + increment > limit)
        {
            return UsageCheckResult.Denied(
                metric,
                current,
                limit,
                $"Usage limit reached for {metric}. Current: {current}, limit: {limit}.");
        }

        return UsageCheckResult.Allowed(metric, current, limit);
    }

    public async Task RecordUsageAsync(
        Guid tenantId,
        UsageMetric metric,
        long increment = 1,
        CancellationToken cancellationToken = default)
    {
        if (increment == 0)
        {
            return;
        }

        if (increment > 0)
        {
            var check = await TryIncrementUsageAsync(tenantId, metric, increment, cancellationToken);
            if (!check.IsAllowed)
            {
                throw new InvalidOperationException(check.DenialReason);
            }

            return;
        }

        var periodKey = GetPeriodKey(metric);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var counter = await dbContext.UsageCounters
                    .SingleOrDefaultAsync(
                        c => c.TenantId == tenantId && c.Metric == metric && c.PeriodKey == periodKey,
                        cancellationToken);

                if (counter is null)
                {
                    return;
                }

                counter.Count = Math.Max(0, counter.Count + increment);
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                dbContext.ChangeTracker.Clear();
            }
        }
    }

    private async Task<UsageCheckResult> TryIncrementUsageAsync(
        Guid tenantId,
        UsageMetric metric,
        long increment,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var check = await CheckUsageAsync(tenantId, metric, increment, cancellationToken);
            if (!check.IsAllowed)
            {
                return check;
            }

            try
            {
                var periodKey = GetPeriodKey(metric);
                var counter = await dbContext.UsageCounters
                    .SingleOrDefaultAsync(
                        c => c.TenantId == tenantId && c.Metric == metric && c.PeriodKey == periodKey,
                        cancellationToken);

                if (counter is null)
                {
                    counter = new UsageCounter
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        Metric = metric,
                        PeriodKey = periodKey,
                        Count = increment
                    };
                    dbContext.UsageCounters.Add(counter);
                }
                else
                {
                    counter.Count += increment;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                return UsageCheckResult.Allowed(metric, counter.Count, check.Limit);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        return UsageCheckResult.Denied(
            metric,
            0,
            0,
            "Unable to record usage due to concurrent update.");
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    public Task AssignDefaultFreePlanAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        AssignPlanAsync(tenantId, PlanCode.Free, EntitlementSource.FreePlan, cancellationToken);

    public async Task AssignPlanAsync(
        Guid tenantId,
        PlanCode planCode,
        EntitlementSource source,
        CancellationToken cancellationToken = default)
    {
        var planVersion = await dbContext.PlanVersions
            .Where(v => v.PlanCode == planCode && v.EffectiveToUtc == null)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (planVersion is null)
        {
            throw new InvalidOperationException($"No active plan version found for {planCode}.");
        }

        var activeAssignments = await dbContext.TenantPlanAssignments
            .Where(a => a.TenantId == tenantId && a.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var assignment in activeAssignments)
        {
            assignment.RevokedAtUtc = now;
        }

        dbContext.TenantPlanAssignments.Add(new TenantPlanAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanVersionId = planVersion.Id,
            Source = source,
            AssignedAtUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TenantPlanAssignment?> GetActiveAssignmentAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await dbContext.TenantPlanAssignments
            .Include(a => a.PlanVersion)
            .Where(a => a.TenantId == tenantId && a.RevokedAtUtc == null)
            .OrderByDescending(a => a.AssignedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyDictionary<UsageMetric, UsageSummary>> BuildUsageSummaryAsync(
        Guid tenantId,
        PlanLimitsSnapshot limits,
        CancellationToken cancellationToken)
    {
        var metrics = new[]
        {
            UsageMetric.SnapshotsCreatedThisMonth,
            UsageMetric.ActiveWorkspaces,
            UsageMetric.StorageBytes,
            UsageMetric.AiExtractionJobsThisMonth,
            UsageMetric.ExternalInvitationsSentThisMonth,
            UsageMetric.ExportDownloadsThisMonth
        };

        var summary = new Dictionary<UsageMetric, UsageSummary>();
        foreach (var metric in metrics)
        {
            var current = await GetCurrentUsageAsync(tenantId, metric, cancellationToken);
            summary[metric] = new UsageSummary
            {
                Current = current,
                Limit = GetLimitForMetric(limits, metric)
            };
        }

        return summary;
    }

    private async Task<long> GetCurrentUsageAsync(
        Guid tenantId,
        UsageMetric metric,
        CancellationToken cancellationToken)
    {
        var periodKey = GetPeriodKey(metric);
        var counter = await dbContext.UsageCounters
            .AsNoTracking()
            .SingleOrDefaultAsync(
                c => c.TenantId == tenantId && c.Metric == metric && c.PeriodKey == periodKey,
                cancellationToken);

        return counter?.Count ?? 0;
    }

    private static long GetLimitForMetric(PlanLimitsSnapshot limits, UsageMetric metric) => metric switch
    {
        UsageMetric.SnapshotsCreatedThisMonth => limits.MaxSnapshotsPerMonth,
        UsageMetric.ActiveWorkspaces => limits.MaxActiveWorkspaces,
        UsageMetric.StorageBytes => limits.MaxStorageBytes,
        UsageMetric.AiExtractionJobsThisMonth => limits.MaxAiExtractionsPerMonth,
        UsageMetric.ExternalInvitationsSentThisMonth => limits.MaxExternalReviewers,
        UsageMetric.ExportDownloadsThisMonth => limits.MaxExportDownloadsPerMonth,
        _ => 0
    };

    private static UsageMetric? MapCapabilityToUsageMetric(Capability capability) => capability switch
    {
        Capability.CanCreateWorkspace => UsageMetric.ActiveWorkspaces,
        Capability.CanCreateSnapshot => UsageMetric.SnapshotsCreatedThisMonth,
        Capability.CanUseAiExtraction => UsageMetric.AiExtractionJobsThisMonth,
        Capability.CanInviteExternalReviewer => UsageMetric.ExternalInvitationsSentThisMonth,
        Capability.CanExportAdvancedPdf => UsageMetric.ExportDownloadsThisMonth,
        _ => null
    };

    private static string GetPeriodKey(UsageMetric metric)
    {
        var now = DateTime.UtcNow;
        return metric switch
        {
            UsageMetric.ActiveWorkspaces => "lifetime",
            UsageMetric.StorageBytes => "lifetime",
            _ => $"{now:yyyy-MM}"
        };
    }
}
