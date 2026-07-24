using Microsoft.EntityFrameworkCore;
using ScopeSeal.AgreementSnapshots.Domain;
using ScopeSeal.AgreementSnapshots.Services;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Audit.Services;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Infrastructure.Persistence;

namespace ScopeSeal.Infrastructure.Services;

public sealed class AgreementSnapshotService(
    ApplicationDbContext dbContext,
    IEntitlementService entitlementService,
    IAuditService auditService) : IAgreementSnapshotService
{
    public async Task<IReadOnlyList<AgreementSnapshotSummary>?> ListSnapshotsAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return null;
        }

        return await dbContext.AgreementSnapshots
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.WorkspaceId == workspaceId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .Select(s => new AgreementSnapshotSummary(
                s.PublicId,
                s.Title,
                s.Description,
                s.Status,
                s.VersionNumber,
                s.CreatedAtUtc,
                s.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<AgreementSnapshotDetail?> GetSnapshotAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return null;
        }

        var snapshot = await LoadSnapshotAsync(tenantId, workspaceId.Value, snapshotPublicId, cancellationToken);
        return snapshot is null ? null : MapDetail(snapshot);
    }

    public async Task<(AgreementSnapshotDetail? Snapshot, string? Error)> CreateSnapshotAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid userId,
        CreateAgreementSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return (null, null);
        }

        var capabilityCheck = await entitlementService.CheckCapabilityAsync(
            tenantId,
            Capability.CanCreateSnapshot,
            cancellationToken);

        if (!capabilityCheck.IsAllowed)
        {
            return (null, capabilityCheck.DenialReason ?? "Snapshot limit reached.");
        }

        var title = request.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return (null, "Title is required.");
        }

        var now = DateTime.UtcNow;
        var snapshot = new AgreementSnapshot
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = workspaceId.Value,
            Title = title,
            Description = request.Description?.Trim(),
            Status = SnapshotStatus.Draft,
            VersionNumber = 1,
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.AgreementSnapshots.Add(snapshot);
        await dbContext.SaveChangesAsync(cancellationToken);

        await entitlementService.RecordUsageAsync(
            tenantId,
            UsageMetric.SnapshotsCreatedThisMonth,
            increment: 1,
            cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.SnapshotCreated,
            "AgreementSnapshot",
            snapshot.PublicId,
            userId,
            $"Agreement snapshot '{snapshot.Title}' created.",
            cancellationToken);

        return (await GetSnapshotAsync(tenantId, workspacePublicId, snapshot.PublicId, cancellationToken), null);
    }

    public async Task<(AgreementSnapshotDetail? Snapshot, string? Error, bool IsConcurrencyConflict)> UpdateSnapshotAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        Guid userId,
        UpdateAgreementSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return (null, null, false);
        }

        dbContext.ChangeTracker.Clear();

        var snapshot = await dbContext.AgreementSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.TenantId == tenantId &&
                     s.WorkspaceId == workspaceId &&
                     s.PublicId == snapshotPublicId,
                cancellationToken);

        if (snapshot is null)
        {
            return (null, null, false);
        }

        if (snapshot.Status != SnapshotStatus.Draft)
        {
            return (null, "Only draft snapshots can be edited.", false);
        }

        if (!TimestampsMatch(snapshot.UpdatedAtUtc, request.ExpectedUpdatedAtUtc))
        {
            return (null, "Snapshot was modified by another session. Refresh and retry.", true);
        }

        var title = request.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return (null, "Title is required.", false);
        }

        var now = DateTime.UtcNow;
        var rowsUpdated = await dbContext.AgreementSnapshots
            .Where(s => s.Id == snapshot.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.Title, title)
                    .SetProperty(s => s.Description, request.Description?.Trim())
                    .SetProperty(s => s.UpdatedAtUtc, now),
                cancellationToken);

        if (rowsUpdated == 0)
        {
            return (null, "Snapshot was modified by another session. Refresh and retry.", true);
        }

        await SyncScopeItemsAsync(snapshot, request.ScopeItems ?? [], now, cancellationToken);
        await SyncSimpleSectionsAsync<Exclusion>(
            snapshot, request.Exclusions ?? [], now,
            () => dbContext.Exclusions.Where(e => e.AgreementSnapshotId == snapshot.Id),
            input => new Exclusion
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshot.TenantId,
                AgreementSnapshotId = snapshot.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            (existing, input) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = now;
            },
            e => e.PublicId,
            cancellationToken);
        await SyncSimpleSectionsAsync<Deliverable>(
            snapshot, request.Deliverables ?? [], now,
            () => dbContext.Deliverables.Where(e => e.AgreementSnapshotId == snapshot.Id),
            input => new Deliverable
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshot.TenantId,
                AgreementSnapshotId = snapshot.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            (existing, input) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = now;
            },
            e => e.PublicId,
            cancellationToken);
        await SyncSimpleSectionsAsync<Commitment>(
            snapshot, request.Commitments ?? [], now,
            () => dbContext.Commitments.Where(e => e.AgreementSnapshotId == snapshot.Id),
            input => new Commitment
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshot.TenantId,
                AgreementSnapshotId = snapshot.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            (existing, input) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = now;
            },
            e => e.PublicId,
            cancellationToken);
        await SyncPaymentMilestonesAsync(snapshot, request.PaymentMilestones ?? [], now, cancellationToken);
        await SyncTimelineMilestonesAsync(snapshot, request.TimelineMilestones ?? [], now, cancellationToken);
        await SyncSimpleSectionsAsync<SnapshotDependency>(
            snapshot, request.Dependencies ?? [], now,
            () => dbContext.SnapshotDependencies.Where(e => e.AgreementSnapshotId == snapshot.Id),
            input => new SnapshotDependency
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshot.TenantId,
                AgreementSnapshotId = snapshot.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            (existing, input) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = now;
            },
            e => e.PublicId,
            cancellationToken);
        await SyncSimpleSectionsAsync<Assumption>(
            snapshot, request.Assumptions ?? [], now,
            () => dbContext.Assumptions.Where(e => e.AgreementSnapshotId == snapshot.Id),
            input => new Assumption
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshot.TenantId,
                AgreementSnapshotId = snapshot.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            (existing, input) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = now;
            },
            e => e.PublicId,
            cancellationToken);
        await SyncSimpleSectionsAsync<OpenQuestion>(
            snapshot, request.OpenQuestions ?? [], now,
            () => dbContext.OpenQuestions.Where(e => e.AgreementSnapshotId == snapshot.Id),
            input => new OpenQuestion
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshot.TenantId,
                AgreementSnapshotId = snapshot.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            (existing, input) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = now;
            },
            e => e.PublicId,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.SnapshotUpdated,
            "AgreementSnapshot",
            snapshot.PublicId,
            userId,
            $"Agreement snapshot '{title}' updated.",
            cancellationToken);

        var detail = await GetSnapshotAsync(tenantId, workspacePublicId, snapshotPublicId, cancellationToken);
        return (detail, null, false);
    }

    private async Task<Guid?> ResolveWorkspaceIdAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId && w.PublicId == workspacePublicId)
            .Select(w => (Guid?)w.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task SyncScopeItemsAsync(
        AgreementSnapshot snapshot,
        IReadOnlyList<SectionItemInput> inputs,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.ScopeItems
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);

        SyncSectionCollection(
            existing,
            inputs,
            i => i.PublicId,
            input => new ScopeItem
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshot.TenantId,
                AgreementSnapshotId = snapshot.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            (entity, input) =>
            {
                entity.SortOrder = input.SortOrder;
                entity.Title = input.Title.Trim();
                entity.Description = input.Description?.Trim();
                entity.UpdatedAtUtc = now;
            },
            entity => dbContext.ScopeItems.Add(entity),
            entity => dbContext.ScopeItems.Remove(entity));
    }

    private async Task SyncSimpleSectionsAsync<TEntity>(
        AgreementSnapshot snapshot,
        IReadOnlyList<SectionItemInput> inputs,
        DateTime now,
        Func<IQueryable<TEntity>> existingQuery,
        Func<SectionItemInput, TEntity> create,
        Action<TEntity, SectionItemInput> update,
        Func<TEntity, Guid> publicIdSelector,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var existing = await existingQuery().ToListAsync(cancellationToken);
        var set = dbContext.Set<TEntity>();
        SyncSectionCollection(
            existing,
            inputs,
            publicIdSelector,
            create,
            update,
            entity => { set.Add(entity); },
            entity => { set.Remove(entity); });
    }

    private async Task SyncPaymentMilestonesAsync(
        AgreementSnapshot snapshot,
        IReadOnlyList<PaymentMilestoneInput> inputs,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.PaymentMilestones
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);

        var existingByPublicId = existing.ToDictionary(i => i.PublicId);
        var retained = new HashSet<Guid>();

        foreach (var input in inputs)
        {
            var title = input.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (input.PublicId is not null && existingByPublicId.TryGetValue(input.PublicId.Value, out var entity))
            {
                entity.SortOrder = input.SortOrder;
                entity.Title = title;
                entity.Description = input.Description?.Trim();
                entity.AmountMinorUnits = input.AmountMinorUnits;
                entity.CurrencyCode = input.CurrencyCode?.Trim().ToUpperInvariant();
                entity.DueDateUtc = NormalizeUtc(input.DueDateUtc);
                entity.UpdatedAtUtc = now;
                retained.Add(entity.PublicId);
            }
            else
            {
                var item = new PaymentMilestone
                {
                    Id = Guid.NewGuid(),
                    PublicId = input.PublicId ?? Guid.NewGuid(),
                    TenantId = snapshot.TenantId,
                    AgreementSnapshotId = snapshot.Id,
                    SortOrder = input.SortOrder,
                    Title = title,
                    Description = input.Description?.Trim(),
                    AmountMinorUnits = input.AmountMinorUnits,
                    CurrencyCode = input.CurrencyCode?.Trim().ToUpperInvariant(),
                    DueDateUtc = NormalizeUtc(input.DueDateUtc),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                dbContext.PaymentMilestones.Add(item);
                retained.Add(item.PublicId);
            }
        }

        foreach (var item in existing.Where(i => !retained.Contains(i.PublicId)).ToList())
        {
            dbContext.PaymentMilestones.Remove(item);
        }
    }

    private async Task SyncTimelineMilestonesAsync(
        AgreementSnapshot snapshot,
        IReadOnlyList<TimelineMilestoneInput> inputs,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.TimelineMilestones
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);

        var existingByPublicId = existing.ToDictionary(i => i.PublicId);
        var retained = new HashSet<Guid>();

        foreach (var input in inputs)
        {
            var title = input.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (input.PublicId is not null && existingByPublicId.TryGetValue(input.PublicId.Value, out var entity))
            {
                entity.SortOrder = input.SortOrder;
                entity.Title = title;
                entity.Description = input.Description?.Trim();
                entity.TargetDateUtc = NormalizeUtc(input.TargetDateUtc);
                entity.UpdatedAtUtc = now;
                retained.Add(entity.PublicId);
            }
            else
            {
                var item = new TimelineMilestone
                {
                    Id = Guid.NewGuid(),
                    PublicId = input.PublicId ?? Guid.NewGuid(),
                    TenantId = snapshot.TenantId,
                    AgreementSnapshotId = snapshot.Id,
                    SortOrder = input.SortOrder,
                    Title = title,
                    Description = input.Description?.Trim(),
                    TargetDateUtc = NormalizeUtc(input.TargetDateUtc),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                dbContext.TimelineMilestones.Add(item);
                retained.Add(item.PublicId);
            }
        }

        foreach (var item in existing.Where(i => !retained.Contains(i.PublicId)).ToList())
        {
            dbContext.TimelineMilestones.Remove(item);
        }
    }

    private static void SyncSectionCollection<TEntity>(
        List<TEntity> existing,
        IReadOnlyList<SectionItemInput> inputs,
        Func<TEntity, Guid> publicIdSelector,
        Func<SectionItemInput, TEntity> create,
        Action<TEntity, SectionItemInput> update,
        Action<TEntity> add,
        Action<TEntity> remove)
        where TEntity : class
    {
        var existingByPublicId = existing.ToDictionary(publicIdSelector);
        var retained = new HashSet<Guid>();

        foreach (var input in inputs)
        {
            var title = input.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (input.PublicId is not null && existingByPublicId.TryGetValue(input.PublicId.Value, out var entity))
            {
                update(entity, input);
                retained.Add(publicIdSelector(entity));
            }
            else
            {
                var item = create(input);
                add(item);
                retained.Add(publicIdSelector(item));
            }
        }

        foreach (var item in existing.Where(i => !retained.Contains(publicIdSelector(i))).ToList())
        {
            remove(item);
        }
    }

    private async Task LoadSnapshotSectionsAsync(
        AgreementSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        await dbContext.Entry(snapshot).Collection(s => s.ScopeItems).LoadAsync(cancellationToken);
        await dbContext.Entry(snapshot).Collection(s => s.Exclusions).LoadAsync(cancellationToken);
        await dbContext.Entry(snapshot).Collection(s => s.Deliverables).LoadAsync(cancellationToken);
        await dbContext.Entry(snapshot).Collection(s => s.Commitments).LoadAsync(cancellationToken);
        await dbContext.Entry(snapshot).Collection(s => s.PaymentMilestones).LoadAsync(cancellationToken);
        await dbContext.Entry(snapshot).Collection(s => s.TimelineMilestones).LoadAsync(cancellationToken);
        await dbContext.Entry(snapshot).Collection(s => s.Dependencies).LoadAsync(cancellationToken);
        await dbContext.Entry(snapshot).Collection(s => s.Assumptions).LoadAsync(cancellationToken);
        await dbContext.Entry(snapshot).Collection(s => s.OpenQuestions).LoadAsync(cancellationToken);
    }

    private async Task<AgreementSnapshot?> LoadSnapshotAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid snapshotPublicId,
        CancellationToken cancellationToken,
        bool tracking = false)
    {
        var query = tracking
            ? dbContext.AgreementSnapshots.AsQueryable()
            : dbContext.AgreementSnapshots.AsNoTracking();

        var snapshot = await query
            .SingleOrDefaultAsync(
                s => s.TenantId == tenantId &&
                     s.WorkspaceId == workspaceId &&
                     s.PublicId == snapshotPublicId,
                cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        if (!tracking)
        {
            await LoadSnapshotSectionsForReadAsync(snapshot, cancellationToken);
        }

        return snapshot;
    }

    private async Task LoadSnapshotSectionsForReadAsync(
        AgreementSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        snapshot.ScopeItems = await dbContext.ScopeItems
            .AsNoTracking()
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.Exclusions = await dbContext.Exclusions
            .AsNoTracking()
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.Deliverables = await dbContext.Deliverables
            .AsNoTracking()
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.Commitments = await dbContext.Commitments
            .AsNoTracking()
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.PaymentMilestones = await dbContext.PaymentMilestones
            .AsNoTracking()
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.TimelineMilestones = await dbContext.TimelineMilestones
            .AsNoTracking()
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.Dependencies = await dbContext.SnapshotDependencies
            .AsNoTracking()
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.Assumptions = await dbContext.Assumptions
            .AsNoTracking()
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.OpenQuestions = await dbContext.OpenQuestions
            .AsNoTracking()
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
    }

    private static AgreementSnapshotDetail MapDetail(AgreementSnapshot snapshot) =>
        new(
            snapshot.PublicId,
            snapshot.Title,
            snapshot.Description,
            snapshot.Status,
            snapshot.VersionNumber,
            snapshot.CreatedAtUtc,
            snapshot.UpdatedAtUtc,
            snapshot.ScopeItems
                .OrderBy(i => i.SortOrder)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.Exclusions
                .OrderBy(i => i.SortOrder)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.Deliverables
                .OrderBy(i => i.SortOrder)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.Commitments
                .OrderBy(i => i.SortOrder)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.PaymentMilestones
                .OrderBy(i => i.SortOrder)
                .Select(i => new PaymentMilestoneDetail(
                    i.PublicId,
                    i.SortOrder,
                    i.Title,
                    i.Description,
                    i.AmountMinorUnits,
                    i.CurrencyCode,
                    i.DueDateUtc))
                .ToList(),
            snapshot.TimelineMilestones
                .OrderBy(i => i.SortOrder)
                .Select(i => new TimelineMilestoneDetail(
                    i.PublicId,
                    i.SortOrder,
                    i.Title,
                    i.Description,
                    i.TargetDateUtc))
                .ToList(),
            snapshot.Dependencies
                .OrderBy(i => i.SortOrder)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.Assumptions
                .OrderBy(i => i.SortOrder)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList(),
            snapshot.OpenQuestions
                .OrderBy(i => i.SortOrder)
                .Select(i => new SectionItemDetail(i.PublicId, i.SortOrder, i.Title, i.Description))
                .ToList());

    private static void SyncSectionItems(
        AgreementSnapshot snapshot,
        IReadOnlyList<SectionItemInput> inputs,
        DateTime now)
    {
        var existingByPublicId = snapshot.ScopeItems.ToDictionary(i => i.PublicId);
        var retained = new HashSet<Guid>();

        foreach (var input in inputs)
        {
            var title = input.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (input.PublicId is not null && existingByPublicId.TryGetValue(input.PublicId.Value, out var existing))
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = title;
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = now;
                retained.Add(existing.PublicId);
            }
            else
            {
                var item = new ScopeItem
                {
                    Id = Guid.NewGuid(),
                    PublicId = input.PublicId ?? Guid.NewGuid(),
                    TenantId = snapshot.TenantId,
                    AgreementSnapshotId = snapshot.Id,
                    SortOrder = input.SortOrder,
                    Title = title,
                    Description = input.Description?.Trim(),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                snapshot.ScopeItems.Add(item);
                retained.Add(item.PublicId);
            }
        }

        RemoveUnretained(snapshot.ScopeItems, retained);
    }

    private static void SyncExclusions(
        AgreementSnapshot snapshot,
        IReadOnlyList<SectionItemInput> inputs,
        DateTime now)
    {
        SyncSimpleSection(
            snapshot.Exclusions,
            inputs,
            snapshot,
            e => e.PublicId,
            (snapshotEntity, input, timestamp) => new Exclusion
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshotEntity.TenantId,
                AgreementSnapshotId = snapshotEntity.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = timestamp,
                UpdatedAtUtc = timestamp
            },
            (existing, input, timestamp) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = timestamp;
            },
            now);
    }

    private static void SyncDeliverables(
        AgreementSnapshot snapshot,
        IReadOnlyList<SectionItemInput> inputs,
        DateTime now) =>
        SyncSimpleSection(
            snapshot.Deliverables,
            inputs,
            snapshot,
            e => e.PublicId,
            (snapshotEntity, input, timestamp) => new Deliverable
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshotEntity.TenantId,
                AgreementSnapshotId = snapshotEntity.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = timestamp,
                UpdatedAtUtc = timestamp
            },
            (existing, input, timestamp) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = timestamp;
            },
            now);

    private static void SyncCommitments(
        AgreementSnapshot snapshot,
        IReadOnlyList<SectionItemInput> inputs,
        DateTime now) =>
        SyncSimpleSection(
            snapshot.Commitments,
            inputs,
            snapshot,
            e => e.PublicId,
            (snapshotEntity, input, timestamp) => new Commitment
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshotEntity.TenantId,
                AgreementSnapshotId = snapshotEntity.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = timestamp,
                UpdatedAtUtc = timestamp
            },
            (existing, input, timestamp) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = timestamp;
            },
            now);

    private static void SyncDependencies(
        AgreementSnapshot snapshot,
        IReadOnlyList<SectionItemInput> inputs,
        DateTime now) =>
        SyncSimpleSection(
            snapshot.Dependencies,
            inputs,
            snapshot,
            e => e.PublicId,
            (snapshotEntity, input, timestamp) => new SnapshotDependency
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshotEntity.TenantId,
                AgreementSnapshotId = snapshotEntity.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = timestamp,
                UpdatedAtUtc = timestamp
            },
            (existing, input, timestamp) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = timestamp;
            },
            now);

    private static void SyncAssumptions(
        AgreementSnapshot snapshot,
        IReadOnlyList<SectionItemInput> inputs,
        DateTime now) =>
        SyncSimpleSection(
            snapshot.Assumptions,
            inputs,
            snapshot,
            e => e.PublicId,
            (snapshotEntity, input, timestamp) => new Assumption
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshotEntity.TenantId,
                AgreementSnapshotId = snapshotEntity.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = timestamp,
                UpdatedAtUtc = timestamp
            },
            (existing, input, timestamp) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = timestamp;
            },
            now);

    private static void SyncOpenQuestions(
        AgreementSnapshot snapshot,
        IReadOnlyList<SectionItemInput> inputs,
        DateTime now) =>
        SyncSimpleSection(
            snapshot.OpenQuestions,
            inputs,
            snapshot,
            e => e.PublicId,
            (snapshotEntity, input, timestamp) => new OpenQuestion
            {
                Id = Guid.NewGuid(),
                PublicId = input.PublicId ?? Guid.NewGuid(),
                TenantId = snapshotEntity.TenantId,
                AgreementSnapshotId = snapshotEntity.Id,
                SortOrder = input.SortOrder,
                Title = input.Title.Trim(),
                Description = input.Description?.Trim(),
                CreatedAtUtc = timestamp,
                UpdatedAtUtc = timestamp
            },
            (existing, input, timestamp) =>
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = input.Title.Trim();
                existing.Description = input.Description?.Trim();
                existing.UpdatedAtUtc = timestamp;
            },
            now);

    private static void SyncPaymentMilestones(
        AgreementSnapshot snapshot,
        IReadOnlyList<PaymentMilestoneInput> inputs,
        DateTime now)
    {
        var existingByPublicId = snapshot.PaymentMilestones.ToDictionary(i => i.PublicId);
        var retained = new HashSet<Guid>();

        foreach (var input in inputs)
        {
            var title = input.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (input.PublicId is not null && existingByPublicId.TryGetValue(input.PublicId.Value, out var existing))
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = title;
                existing.Description = input.Description?.Trim();
                existing.AmountMinorUnits = input.AmountMinorUnits;
                existing.CurrencyCode = input.CurrencyCode?.Trim().ToUpperInvariant();
                existing.DueDateUtc = NormalizeUtc(input.DueDateUtc);
                existing.UpdatedAtUtc = now;
                retained.Add(existing.PublicId);
            }
            else
            {
                var item = new PaymentMilestone
                {
                    Id = Guid.NewGuid(),
                    PublicId = input.PublicId ?? Guid.NewGuid(),
                    TenantId = snapshot.TenantId,
                    AgreementSnapshotId = snapshot.Id,
                    SortOrder = input.SortOrder,
                    Title = title,
                    Description = input.Description?.Trim(),
                    AmountMinorUnits = input.AmountMinorUnits,
                    CurrencyCode = input.CurrencyCode?.Trim().ToUpperInvariant(),
                    DueDateUtc = NormalizeUtc(input.DueDateUtc),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                snapshot.PaymentMilestones.Add(item);
                retained.Add(item.PublicId);
            }
        }

        RemoveUnretained(snapshot.PaymentMilestones, retained);
    }

    private static void SyncTimelineMilestones(
        AgreementSnapshot snapshot,
        IReadOnlyList<TimelineMilestoneInput> inputs,
        DateTime now)
    {
        var existingByPublicId = snapshot.TimelineMilestones.ToDictionary(i => i.PublicId);
        var retained = new HashSet<Guid>();

        foreach (var input in inputs)
        {
            var title = input.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (input.PublicId is not null && existingByPublicId.TryGetValue(input.PublicId.Value, out var existing))
            {
                existing.SortOrder = input.SortOrder;
                existing.Title = title;
                existing.Description = input.Description?.Trim();
                existing.TargetDateUtc = NormalizeUtc(input.TargetDateUtc);
                existing.UpdatedAtUtc = now;
                retained.Add(existing.PublicId);
            }
            else
            {
                var item = new TimelineMilestone
                {
                    Id = Guid.NewGuid(),
                    PublicId = input.PublicId ?? Guid.NewGuid(),
                    TenantId = snapshot.TenantId,
                    AgreementSnapshotId = snapshot.Id,
                    SortOrder = input.SortOrder,
                    Title = title,
                    Description = input.Description?.Trim(),
                    TargetDateUtc = NormalizeUtc(input.TargetDateUtc),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                snapshot.TimelineMilestones.Add(item);
                retained.Add(item.PublicId);
            }
        }

        RemoveUnretained(snapshot.TimelineMilestones, retained);
    }

    private static void SyncSimpleSection<TEntity>(
        ICollection<TEntity> collection,
        IReadOnlyList<SectionItemInput> inputs,
        AgreementSnapshot snapshot,
        Func<TEntity, Guid> publicIdSelector,
        Func<AgreementSnapshot, SectionItemInput, DateTime, TEntity> create,
        Action<TEntity, SectionItemInput, DateTime> update,
        DateTime now)
        where TEntity : class
    {
        var existingByPublicId = collection.ToDictionary(publicIdSelector);
        var retained = new HashSet<Guid>();

        foreach (var input in inputs)
        {
            var title = input.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (input.PublicId is not null && existingByPublicId.TryGetValue(input.PublicId.Value, out var existing))
            {
                update(existing, input, now);
                retained.Add(publicIdSelector(existing));
            }
            else
            {
                var item = create(snapshot, input, now);
                collection.Add(item);
                retained.Add(publicIdSelector(item));
            }
        }

        RemoveUnretained(collection, retained, publicIdSelector);
    }

    private static bool TimestampsMatch(DateTime stored, DateTime expected)
    {
        var storedUtc = NormalizeUtc(stored)!.Value;
        var expectedUtc = NormalizeUtc(expected)!.Value;
        return storedUtc.Ticks / TimeSpan.TicksPerMillisecond ==
               expectedUtc.Ticks / TimeSpan.TicksPerMillisecond;
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

    private static void RemoveUnretained(ICollection<ScopeItem> items, HashSet<Guid> retained)
    {
        var toRemove = items.Where(i => !retained.Contains(i.PublicId)).ToList();
        foreach (var item in toRemove)
        {
            items.Remove(item);
        }
    }

    private static void RemoveUnretained(ICollection<PaymentMilestone> items, HashSet<Guid> retained)
    {
        var toRemove = items.Where(i => !retained.Contains(i.PublicId)).ToList();
        foreach (var item in toRemove)
        {
            items.Remove(item);
        }
    }

    private static void RemoveUnretained(ICollection<TimelineMilestone> items, HashSet<Guid> retained)
    {
        var toRemove = items.Where(i => !retained.Contains(i.PublicId)).ToList();
        foreach (var item in toRemove)
        {
            items.Remove(item);
        }
    }

    private static void RemoveUnretained<TEntity>(
        ICollection<TEntity> items,
        HashSet<Guid> retained,
        Func<TEntity, Guid> publicIdSelector)
    {
        var toRemove = items.Where(i => !retained.Contains(publicIdSelector(i))).ToList();
        foreach (var item in toRemove)
        {
            items.Remove(item);
        }
    }
}
