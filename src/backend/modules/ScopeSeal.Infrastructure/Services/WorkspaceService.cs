using Microsoft.EntityFrameworkCore;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Audit.Services;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Workspaces.Domain;
using ScopeSeal.Workspaces.Services;

namespace ScopeSeal.Infrastructure.Services;

public sealed class WorkspaceService(
    ApplicationDbContext dbContext,
    IEntitlementService entitlementService,
    IAuditService auditService) : IWorkspaceService
{
    public async Task<IReadOnlyList<WorkspaceSummary>> ListWorkspacesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId)
            .OrderByDescending(w => w.UpdatedAtUtc)
            .Select(w => new WorkspaceSummary(
                w.PublicId,
                w.Name,
                w.Description,
                w.Type,
                w.Status,
                w.CreatedAtUtc,
                w.UpdatedAtUtc,
                w.Parties.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkspaceDetail?> GetWorkspaceAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken = default)
    {
        var workspace = await dbContext.Workspaces
            .AsNoTracking()
            .Include(w => w.Template)
            .Include(w => w.Parties)
                .ThenInclude(wp => wp.Party)
                    .ThenInclude(p => p.Contact)
            .SingleOrDefaultAsync(
                w => w.TenantId == tenantId && w.PublicId == workspacePublicId,
                cancellationToken);

        return workspace is null ? null : MapDetail(workspace);
    }

    public async Task<(WorkspaceDetail? Workspace, string? Error)> CreateWorkspaceAsync(
        Guid tenantId,
        Guid userId,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var capabilityCheck = await entitlementService.CheckCapabilityAsync(
            tenantId,
            Capability.CanCreateWorkspace,
            cancellationToken);

        if (!capabilityCheck.IsAllowed)
        {
            return (null, capabilityCheck.DenialReason ?? "Workspace limit reached.");
        }

        Guid? templateId = null;
        if (request.TemplatePublicId is not null)
        {
            var template = await dbContext.WorkspaceTemplates
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    t => t.PublicId == request.TemplatePublicId &&
                         (t.IsSystem || t.TenantId == tenantId),
                    cancellationToken);

            if (template is null)
            {
                return (null, "Template not found.");
            }

            templateId = template.Id;
        }

        var now = DateTime.UtcNow;
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Type = request.Type,
            Status = WorkspaceStatus.Draft,
            TemplateId = templateId,
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync(cancellationToken);

        await entitlementService.RecordUsageAsync(
            tenantId,
            UsageMetric.ActiveWorkspaces,
            increment: 1,
            cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.WorkspaceCreated,
            "Workspace",
            workspace.PublicId,
            userId,
            $"Workspace '{workspace.Name}' created.",
            cancellationToken);

        return (await GetWorkspaceAsync(tenantId, workspace.PublicId, cancellationToken), null);
    }

    public async Task<(WorkspaceDetail? Workspace, string? Error)> UpdateWorkspaceAsync(
        Guid tenantId,
        Guid workspacePublicId,
        UpdateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspace = await dbContext.Workspaces
            .SingleOrDefaultAsync(
                w => w.TenantId == tenantId && w.PublicId == workspacePublicId,
                cancellationToken);

        if (workspace is null)
        {
            return (null, null);
        }

        if (workspace.Status == WorkspaceStatus.Archived)
        {
            return (null, "Archived workspaces cannot be updated.");
        }

        workspace.Name = request.Name.Trim();
        workspace.Description = request.Description?.Trim();
        workspace.Type = request.Type;
        workspace.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.WorkspaceUpdated,
            "Workspace",
            workspace.PublicId,
            summary: $"Workspace '{workspace.Name}' updated.",
            cancellationToken: cancellationToken);

        return (await GetWorkspaceAsync(tenantId, workspace.PublicId, cancellationToken), null);
    }

    public async Task<(WorkspaceDetail? Workspace, string? Error)> ArchiveWorkspaceAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken = default)
    {
        var workspace = await dbContext.Workspaces
            .SingleOrDefaultAsync(
                w => w.TenantId == tenantId && w.PublicId == workspacePublicId,
                cancellationToken);

        if (workspace is null)
        {
            return (null, null);
        }

        if (workspace.Status == WorkspaceStatus.Archived)
        {
            return (await GetWorkspaceAsync(tenantId, workspace.PublicId, cancellationToken), null);
        }

        workspace.Status = WorkspaceStatus.Archived;
        workspace.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await entitlementService.RecordUsageAsync(
            tenantId,
            UsageMetric.ActiveWorkspaces,
            increment: -1,
            cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.WorkspaceArchived,
            "Workspace",
            workspace.PublicId,
            summary: $"Workspace '{workspace.Name}' archived.",
            cancellationToken: cancellationToken);

        return (await GetWorkspaceAsync(tenantId, workspace.PublicId, cancellationToken), null);
    }

    public async Task<(WorkspacePartySummary? Party, string? Error)> AddPartyToWorkspaceAsync(
        Guid tenantId,
        Guid workspacePublicId,
        AddWorkspacePartyRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspace = await dbContext.Workspaces
            .SingleOrDefaultAsync(
                w => w.TenantId == tenantId && w.PublicId == workspacePublicId,
                cancellationToken);

        if (workspace is null)
        {
            return (null, null);
        }

        if (workspace.Status == WorkspaceStatus.Archived)
        {
            return (null, "Archived workspaces cannot be modified.");
        }

        var party = await dbContext.Parties
            .Include(p => p.Contact)
            .SingleOrDefaultAsync(
                p => p.TenantId == tenantId && p.PublicId == request.PartyPublicId,
                cancellationToken);

        if (party is null)
        {
            return (null, "Party not found.");
        }

        var existing = await dbContext.WorkspaceParties
            .AnyAsync(
                wp => wp.WorkspaceId == workspace.Id && wp.PartyId == party.Id,
                cancellationToken);

        if (existing)
        {
            return (null, "Party is already linked to this workspace.");
        }

        var workspaceParty = new WorkspaceParty
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            PartyId = party.Id,
            Role = request.Role,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.WorkspaceParties.Add(workspaceParty);
        workspace.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.WorkspacePartyAdded,
            "WorkspaceParty",
            party.PublicId,
            summary: $"Party '{party.DisplayName}' added to workspace '{workspace.Name}'.",
            cancellationToken: cancellationToken);

        return (new WorkspacePartySummary(
            party.PublicId,
            party.DisplayName,
            request.Role,
            party.RoleLabel,
            party.Contact?.Email), null);
    }

    private static WorkspaceDetail MapDetail(Workspace workspace) => new(
        workspace.PublicId,
        workspace.Name,
        workspace.Description,
        workspace.Type,
        workspace.Status,
        workspace.Template?.PublicId,
        workspace.CreatedAtUtc,
        workspace.UpdatedAtUtc,
        workspace.Parties
            .OrderBy(wp => wp.CreatedAtUtc)
            .Select(wp => new WorkspacePartySummary(
                wp.Party.PublicId,
                wp.Party.DisplayName,
                wp.Role,
                wp.Party.RoleLabel,
                wp.Party.Contact?.Email))
            .ToList());
}
