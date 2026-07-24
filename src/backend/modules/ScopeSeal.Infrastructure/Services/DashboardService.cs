using Microsoft.EntityFrameworkCore;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Workspaces.Domain;
using ScopeSeal.Workspaces.Services;

namespace ScopeSeal.Infrastructure.Services;

public sealed class DashboardService(
    ApplicationDbContext dbContext,
    IEntitlementService entitlementService) : IDashboardService
{
    public async Task<DashboardSummary?> GetDashboardAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var entitlementSummary = await entitlementService.GetSummaryAsync(tenantId, cancellationToken);
        if (entitlementSummary is null)
        {
            return null;
        }

        var workspaces = await dbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId)
            .OrderByDescending(w => w.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        var totalContacts = await dbContext.Contacts
            .AsNoTracking()
            .CountAsync(c => c.TenantId == tenantId, cancellationToken);

        var totalParties = await dbContext.Parties
            .AsNoTracking()
            .CountAsync(p => p.TenantId == tenantId, cancellationToken);

        var activeWorkspaces = workspaces.Count(w => w.Status != WorkspaceStatus.Archived);
        var usage = entitlementSummary.Usage.TryGetValue(UsageMetric.ActiveWorkspaces, out var workspaceUsage)
            ? workspaceUsage
            : new UsageSummary { Current = 0, Limit = 0 };

        var recent = workspaces
            .Take(5)
            .Select(w => new WorkspaceSummary(
                w.PublicId,
                w.Name,
                w.Description,
                w.Type,
                w.Status,
                w.CreatedAtUtc,
                w.UpdatedAtUtc,
                0))
            .ToList();

        return new DashboardSummary(
            workspaces.Count,
            activeWorkspaces,
            workspaces.Count(w => w.Status == WorkspaceStatus.Draft),
            workspaces.Count(w => w.Status == WorkspaceStatus.Archived),
            totalContacts,
            totalParties,
            usage.Limit,
            usage.Current,
            recent);
    }
}

public sealed class WorkspaceTemplateService(ApplicationDbContext dbContext) : IWorkspaceTemplateService
{
    public async Task<IReadOnlyList<WorkspaceTemplateSummary>> ListTemplatesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkspaceTemplates
            .AsNoTracking()
            .Where(t => t.IsSystem || t.TenantId == tenantId)
            .OrderBy(t => t.IsSystem ? 0 : 1)
            .ThenBy(t => t.Name)
            .Select(t => new WorkspaceTemplateSummary(
                t.PublicId,
                t.Name,
                t.Description,
                t.WorkspaceType,
                t.IsSystem))
            .ToListAsync(cancellationToken);
    }
}
