using Microsoft.EntityFrameworkCore;
using ScopeSeal.AgreementSnapshots.Domain;
using ScopeSeal.AgreementSnapshots.Services;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Audit.Services;
using ScopeSeal.ChangeLedger.Domain;
using ScopeSeal.ChangeLedger.Services;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Infrastructure.Persistence;

namespace ScopeSeal.Infrastructure.Services;

public sealed class ChangeLedgerService(
    ApplicationDbContext dbContext,
    IAgreementSnapshotService snapshotService,
    IEntitlementService entitlementService,
    IAuditService auditService) : IChangeLedgerService
{
    public async Task<(ChangeRequestDetail? ChangeRequest, string? Error)> CreateChangeRequestAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid userId,
        CreateChangeRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var capabilityCheck = await entitlementService.CheckCapabilityAsync(
            tenantId,
            Capability.CanUseChangeRequestWorkflow,
            cancellationToken);

        if (!capabilityCheck.IsAllowed)
        {
            return (null, capabilityCheck.DenialReason ?? "Change request workflow is not available for this plan.");
        }

        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return (null, null);
        }

        var sourceSnapshot = await dbContext.AgreementSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.TenantId == tenantId &&
                     s.WorkspaceId == workspaceId &&
                     s.PublicId == request.SourceSnapshotPublicId,
                cancellationToken);

        if (sourceSnapshot is null)
        {
            return (null, null);
        }

        if (sourceSnapshot.Status != SnapshotStatus.Approved)
        {
            return (null, "Change requests can only be created against approved snapshots.");
        }

        var title = request.Title.Trim();
        var reason = request.Reason.Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(reason))
        {
            return (null, "Title and reason are required.");
        }

        var now = DateTime.UtcNow;
        var changeRequest = new ChangeRequest
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = workspaceId.Value,
            SourceSnapshotId = sourceSnapshot.Id,
            Title = title,
            Reason = reason,
            Status = ChangeRequestStatus.Proposed,
            ProposedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var impactInput in request.Impacts ?? [])
        {
            var description = impactInput.Description.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            changeRequest.Impacts.Add(new ChangeImpact
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = tenantId,
                ChangeRequestId = changeRequest.Id,
                ImpactType = impactInput.ImpactType,
                Description = description,
                AmountMinorUnits = impactInput.AmountMinorUnits,
                CurrencyCode = impactInput.CurrencyCode?.Trim().ToUpperInvariant(),
                ScheduleDaysDelta = impactInput.ScheduleDaysDelta
            });
        }

        dbContext.ChangeRequests.Add(changeRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.ChangeRequestCreated,
            "ChangeRequest",
            changeRequest.PublicId,
            userId,
            $"Change request '{changeRequest.Title}' proposed.",
            cancellationToken);

        var detail = await GetChangeRequestAsync(tenantId, workspacePublicId, changeRequest.PublicId, cancellationToken);
        return (detail, null);
    }

    public async Task<IReadOnlyList<ChangeRequestSummary>?> ListChangeRequestsAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return null;
        }

        var requests = await dbContext.ChangeRequests
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.WorkspaceId == workspaceId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var snapshotIds = requests
            .Select(r => r.SourceSnapshotId)
            .Concat(requests.Where(r => r.ResultSnapshotId.HasValue).Select(r => r.ResultSnapshotId!.Value))
            .Distinct()
            .ToList();

        var snapshotPublicIds = await dbContext.AgreementSnapshots
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && snapshotIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.PublicId, cancellationToken);

        return requests.Select(r => new ChangeRequestSummary(
            r.PublicId,
            r.Title,
            r.Status,
            snapshotPublicIds[r.SourceSnapshotId],
            r.ResultSnapshotId is not null ? snapshotPublicIds.GetValueOrDefault(r.ResultSnapshotId.Value) : null,
            r.CreatedAtUtc,
            r.UpdatedAtUtc)).ToList();
    }

    public async Task<ChangeRequestDetail?> GetChangeRequestAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid changeRequestPublicId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return null;
        }

        var changeRequest = await dbContext.ChangeRequests
            .AsNoTracking()
            .Include(r => r.Impacts)
            .Include(r => r.Decisions)
            .SingleOrDefaultAsync(
                r => r.TenantId == tenantId &&
                     r.WorkspaceId == workspaceId &&
                     r.PublicId == changeRequestPublicId,
                cancellationToken);

        if (changeRequest is null)
        {
            return null;
        }

        var snapshotIds = new List<Guid> { changeRequest.SourceSnapshotId };
        if (changeRequest.ResultSnapshotId is not null)
        {
            snapshotIds.Add(changeRequest.ResultSnapshotId.Value);
        }

        var snapshots = await dbContext.AgreementSnapshots
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && snapshotIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        if (!snapshots.TryGetValue(changeRequest.SourceSnapshotId, out var source))
        {
            return null;
        }

        AgreementSnapshot? result = null;
        if (changeRequest.ResultSnapshotId is not null)
        {
            snapshots.TryGetValue(changeRequest.ResultSnapshotId.Value, out result);
        }

        return MapDetail(changeRequest, source.PublicId, source.VersionNumber, result?.PublicId, result?.VersionNumber);
    }

    public async Task<(ChangeRequestDetail? ChangeRequest, string? Error)> TransitionChangeRequestAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid changeRequestPublicId,
        Guid userId,
        TransitionChangeRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return (null, null);
        }

        var changeRequest = await dbContext.ChangeRequests
            .SingleOrDefaultAsync(
                r => r.TenantId == tenantId &&
                     r.WorkspaceId == workspaceId &&
                     r.PublicId == changeRequestPublicId,
                cancellationToken);

        if (changeRequest is null)
        {
            return (null, null);
        }

        if (request.NewStatus == ChangeRequestStatus.Accepted)
        {
            return (null, "Use the accept endpoint to accept a change request and create a draft snapshot.");
        }

        if (!IsValidTransition(changeRequest.Status, request.NewStatus))
        {
            return (null, $"Cannot transition from {changeRequest.Status} to {request.NewStatus}.");
        }

        var now = DateTime.UtcNow;
        var previousStatus = changeRequest.Status;
        changeRequest.Status = request.NewStatus;
        changeRequest.UpdatedAtUtc = now;

        dbContext.ChangeDecisions.Add(new ChangeDecision
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            ChangeRequestId = changeRequest.Id,
            DecidedByUserId = userId,
            DecisionNote = request.DecisionNote?.Trim(),
            PreviousStatus = previousStatus,
            NewStatus = request.NewStatus,
            DecidedAtUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.ChangeRequestStatusChanged,
            "ChangeRequest",
            changeRequest.PublicId,
            userId,
            $"Change request status changed from {previousStatus} to {request.NewStatus}.",
            cancellationToken);

        var detail = await GetChangeRequestAsync(tenantId, workspacePublicId, changeRequestPublicId, cancellationToken);
        return (detail, null);
    }

    public async Task<(AcceptChangeRequestResult? Result, string? Error)> AcceptChangeRequestAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid changeRequestPublicId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return (null, null);
        }

        var changeRequest = await dbContext.ChangeRequests
            .SingleOrDefaultAsync(
                r => r.TenantId == tenantId &&
                     r.WorkspaceId == workspaceId &&
                     r.PublicId == changeRequestPublicId,
                cancellationToken);

        if (changeRequest is null)
        {
            return (null, null);
        }

        if (changeRequest.ResultSnapshotId is not null)
        {
            return (null, "This change request has already been accepted.");
        }

        if (!CanAcceptFromStatus(changeRequest.Status))
        {
            return (null, $"Change request in status {changeRequest.Status} cannot be accepted.");
        }

        var sourceSnapshot = await LoadSnapshotWithSectionsAsync(
            tenantId, changeRequest.SourceSnapshotId, cancellationToken);

        if (sourceSnapshot is null || sourceSnapshot.Status != SnapshotStatus.Approved)
        {
            return (null, "Source snapshot is no longer approved.");
        }

        var now = DateTime.UtcNow;
        var previousStatus = changeRequest.Status;
        var draftSnapshot = CloneSnapshotAsDraft(sourceSnapshot, changeRequest, userId, now);

        dbContext.AgreementSnapshots.Add(draftSnapshot);
        changeRequest.Status = ChangeRequestStatus.Accepted;
        changeRequest.ResultSnapshotId = draftSnapshot.Id;
        changeRequest.UpdatedAtUtc = now;

        dbContext.ChangeDecisions.Add(new ChangeDecision
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            ChangeRequestId = changeRequest.Id,
            DecidedByUserId = userId,
            DecisionNote = "Change request accepted; draft snapshot created.",
            PreviousStatus = previousStatus,
            NewStatus = ChangeRequestStatus.Accepted,
            DecidedAtUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.ChangeRequestAccepted,
            "ChangeRequest",
            changeRequest.PublicId,
            userId,
            $"Change request accepted; draft snapshot v{draftSnapshot.VersionNumber} created.",
            cancellationToken);

        var changeRequestDetail = await GetChangeRequestAsync(
            tenantId, workspacePublicId, changeRequestPublicId, cancellationToken);

        var draftDetail = await snapshotService.GetSnapshotAsync(
            tenantId, workspacePublicId, draftSnapshot.PublicId, cancellationToken);

        return (new AcceptChangeRequestResult(changeRequestDetail!, draftDetail!), null);
    }

    public async Task<SnapshotDiffDetail?> GetSnapshotDiffAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid fromSnapshotPublicId,
        Guid toSnapshotPublicId,
        CancellationToken cancellationToken = default)
    {
        var fromDetail = await snapshotService.GetSnapshotAsync(
            tenantId, workspacePublicId, fromSnapshotPublicId, cancellationToken);

        var toDetail = await snapshotService.GetSnapshotAsync(
            tenantId, workspacePublicId, toSnapshotPublicId, cancellationToken);

        if (fromDetail is null || toDetail is null)
        {
            return null;
        }

        return SnapshotDiffService.ComputeDiff(fromDetail, toDetail);
    }

    internal static async Task MarkChangeRequestImplementedAsync(
        ApplicationDbContext dbContext,
        IAuditService auditService,
        Guid tenantId,
        Guid changeRequestId,
        CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests
            .SingleOrDefaultAsync(
                r => r.TenantId == tenantId && r.Id == changeRequestId,
                cancellationToken);

        if (changeRequest is null || changeRequest.Status != ChangeRequestStatus.Accepted)
        {
            return;
        }

        var now = DateTime.UtcNow;
        changeRequest.Status = ChangeRequestStatus.Implemented;
        changeRequest.ImplementedAtUtc = now;
        changeRequest.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.ChangeRequestImplemented,
            "ChangeRequest",
            changeRequest.PublicId,
            null,
            "Change request marked implemented after re-approval.",
            cancellationToken);
    }

    private static AgreementSnapshot CloneSnapshotAsDraft(
        AgreementSnapshot source,
        ChangeRequest changeRequest,
        Guid userId,
        DateTime now)
    {
        var draft = new AgreementSnapshot
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = source.TenantId,
            WorkspaceId = source.WorkspaceId,
            Title = source.Title,
            Description = source.Description,
            Status = SnapshotStatus.Draft,
            VersionNumber = source.VersionNumber + 1,
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            SourceSnapshotId = source.Id,
            ChangeRequestId = changeRequest.Id
        };

        foreach (var item in source.ScopeItems.OrderBy(i => i.SortOrder))
        {
            draft.ScopeItems.Add(new ScopeItem
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = draft.TenantId,
                AgreementSnapshotId = draft.Id,
                SortOrder = item.SortOrder,
                Title = item.Title,
                Description = item.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        foreach (var item in source.Exclusions.OrderBy(i => i.SortOrder))
        {
            draft.Exclusions.Add(new Exclusion
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = draft.TenantId,
                AgreementSnapshotId = draft.Id,
                SortOrder = item.SortOrder,
                Title = item.Title,
                Description = item.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        foreach (var item in source.Deliverables.OrderBy(i => i.SortOrder))
        {
            draft.Deliverables.Add(new Deliverable
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = draft.TenantId,
                AgreementSnapshotId = draft.Id,
                SortOrder = item.SortOrder,
                Title = item.Title,
                Description = item.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        foreach (var item in source.Commitments.OrderBy(i => i.SortOrder))
        {
            draft.Commitments.Add(new Commitment
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = draft.TenantId,
                AgreementSnapshotId = draft.Id,
                SortOrder = item.SortOrder,
                Title = item.Title,
                Description = item.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        foreach (var item in source.PaymentMilestones.OrderBy(i => i.SortOrder))
        {
            draft.PaymentMilestones.Add(new PaymentMilestone
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = draft.TenantId,
                AgreementSnapshotId = draft.Id,
                SortOrder = item.SortOrder,
                Title = item.Title,
                Description = item.Description,
                AmountMinorUnits = item.AmountMinorUnits,
                CurrencyCode = item.CurrencyCode,
                DueDateUtc = item.DueDateUtc,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        foreach (var item in source.TimelineMilestones.OrderBy(i => i.SortOrder))
        {
            draft.TimelineMilestones.Add(new TimelineMilestone
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = draft.TenantId,
                AgreementSnapshotId = draft.Id,
                SortOrder = item.SortOrder,
                Title = item.Title,
                Description = item.Description,
                TargetDateUtc = item.TargetDateUtc,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        foreach (var item in source.Dependencies.OrderBy(i => i.SortOrder))
        {
            draft.Dependencies.Add(new SnapshotDependency
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = draft.TenantId,
                AgreementSnapshotId = draft.Id,
                SortOrder = item.SortOrder,
                Title = item.Title,
                Description = item.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        foreach (var item in source.Assumptions.OrderBy(i => i.SortOrder))
        {
            draft.Assumptions.Add(new Assumption
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = draft.TenantId,
                AgreementSnapshotId = draft.Id,
                SortOrder = item.SortOrder,
                Title = item.Title,
                Description = item.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        foreach (var item in source.OpenQuestions.OrderBy(i => i.SortOrder))
        {
            draft.OpenQuestions.Add(new OpenQuestion
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = draft.TenantId,
                AgreementSnapshotId = draft.Id,
                SortOrder = item.SortOrder,
                Title = item.Title,
                Description = item.Description,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        return draft;
    }

    private async Task<AgreementSnapshot?> LoadSnapshotWithSectionsAsync(
        Guid tenantId,
        Guid snapshotId,
        CancellationToken cancellationToken)
    {
        var snapshot = await dbContext.AgreementSnapshots
            .SingleOrDefaultAsync(
                s => s.TenantId == tenantId && s.Id == snapshotId,
                cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        snapshot.ScopeItems = await dbContext.ScopeItems
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.Exclusions = await dbContext.Exclusions
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.Deliverables = await dbContext.Deliverables
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.Commitments = await dbContext.Commitments
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.PaymentMilestones = await dbContext.PaymentMilestones
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.TimelineMilestones = await dbContext.TimelineMilestones
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.Dependencies = await dbContext.SnapshotDependencies
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.Assumptions = await dbContext.Assumptions
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);
        snapshot.OpenQuestions = await dbContext.OpenQuestions
            .Where(i => i.AgreementSnapshotId == snapshot.Id)
            .ToListAsync(cancellationToken);

        return snapshot;
    }

    private async Task<Guid?> ResolveWorkspaceIdAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken) =>
        await dbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId && w.PublicId == workspacePublicId)
            .Select(w => (Guid?)w.Id)
            .SingleOrDefaultAsync(cancellationToken);

    private static ChangeRequestDetail MapDetail(
        ChangeRequest changeRequest,
        Guid sourceSnapshotPublicId,
        int sourceSnapshotVersionNumber,
        Guid? resultSnapshotPublicId,
        int? resultSnapshotVersionNumber) =>
        new(
            changeRequest.PublicId,
            changeRequest.Title,
            changeRequest.Reason,
            changeRequest.Status,
            sourceSnapshotPublicId,
            sourceSnapshotVersionNumber,
            resultSnapshotPublicId,
            resultSnapshotVersionNumber,
            changeRequest.Impacts
                .Select(i => new ChangeImpactDetail(
                    i.PublicId,
                    i.ImpactType,
                    i.Description,
                    i.AmountMinorUnits,
                    i.CurrencyCode,
                    i.ScheduleDaysDelta))
                .ToList(),
            changeRequest.Decisions
                .OrderBy(d => d.DecidedAtUtc)
                .Select(d => new ChangeDecisionDetail(
                    d.PublicId,
                    d.PreviousStatus,
                    d.NewStatus,
                    d.DecisionNote,
                    d.DecidedAtUtc))
                .ToList(),
            changeRequest.CreatedAtUtc,
            changeRequest.UpdatedAtUtc,
            changeRequest.ImplementedAtUtc);

    private static bool CanAcceptFromStatus(ChangeRequestStatus status) =>
        status is ChangeRequestStatus.UnderDiscussion
            or ChangeRequestStatus.PricingRequired
            or ChangeRequestStatus.ScheduleReviewRequired;

    private static bool IsValidTransition(ChangeRequestStatus current, ChangeRequestStatus next)
    {
        if (current == next)
        {
            return false;
        }

        return current switch
        {
            ChangeRequestStatus.Proposed => next is ChangeRequestStatus.UnderDiscussion
                or ChangeRequestStatus.Withdrawn,
            ChangeRequestStatus.UnderDiscussion => next is ChangeRequestStatus.PricingRequired
                or ChangeRequestStatus.ScheduleReviewRequired
                or ChangeRequestStatus.Rejected
                or ChangeRequestStatus.Withdrawn,
            ChangeRequestStatus.PricingRequired => next is ChangeRequestStatus.UnderDiscussion
                or ChangeRequestStatus.ScheduleReviewRequired
                or ChangeRequestStatus.Rejected
                or ChangeRequestStatus.Withdrawn,
            ChangeRequestStatus.ScheduleReviewRequired => next is ChangeRequestStatus.UnderDiscussion
                or ChangeRequestStatus.PricingRequired
                or ChangeRequestStatus.Rejected
                or ChangeRequestStatus.Withdrawn,
            ChangeRequestStatus.Accepted => next is ChangeRequestStatus.Implemented,
            ChangeRequestStatus.Rejected or ChangeRequestStatus.Withdrawn
                or ChangeRequestStatus.Implemented => false,
            _ => false
        };
    }
}
