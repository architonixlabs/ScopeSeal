using Microsoft.EntityFrameworkCore;
using ScopeSeal.AgreementSnapshots.Domain;
using ScopeSeal.AgreementSnapshots.Services;
using ScopeSeal.Approvals.Domain;
using ScopeSeal.Approvals.Services;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Audit.Services;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Infrastructure.Persistence;

namespace ScopeSeal.Infrastructure.Services;

public sealed class ReviewApprovalService(
    ApplicationDbContext dbContext,
    IAgreementSnapshotService snapshotService,
    IEntitlementService entitlementService,
    IAuditService auditService) : IReviewApprovalService
{
    private const int DefaultInvitationExpirationDays = 7;
    private const int MinConfirmationLength = 10;

    public async Task<(AgreementSnapshotDetail? Snapshot, string? Error)> ShareSnapshotAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await ResolveSnapshotAsync(tenantId, workspacePublicId, snapshotPublicId, cancellationToken);
        if (snapshot is null)
        {
            return (null, null);
        }

        if (snapshot.Status != SnapshotStatus.Draft)
        {
            return (null, "Only draft snapshots can be shared for review.");
        }

        var now = DateTime.UtcNow;
        await dbContext.AgreementSnapshots
            .Where(s => s.Id == snapshot.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.Status, SnapshotStatus.Shared)
                    .SetProperty(s => s.UpdatedAtUtc, now),
                cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.SnapshotShared,
            "AgreementSnapshot",
            snapshot.PublicId,
            userId,
            $"Agreement snapshot '{snapshot.Title}' shared for review.",
            cancellationToken);

        return (await snapshotService.GetSnapshotAsync(tenantId, workspacePublicId, snapshotPublicId, cancellationToken), null);
    }

    public async Task<(AgreementSnapshotDetail? Snapshot, string? Error)> MarkReadyForApprovalAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await ResolveSnapshotAsync(tenantId, workspacePublicId, snapshotPublicId, cancellationToken);
        if (snapshot is null)
        {
            return (null, null);
        }

        if (snapshot.Status is not (SnapshotStatus.Shared or SnapshotStatus.ChangesRequested))
        {
            return (null, "Snapshot must be shared or have changes requested before marking ready for approval.");
        }

        var now = DateTime.UtcNow;
        await dbContext.AgreementSnapshots
            .Where(s => s.Id == snapshot.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.Status, SnapshotStatus.ReadyForApproval)
                    .SetProperty(s => s.UpdatedAtUtc, now),
                cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.SnapshotReadyForApproval,
            "AgreementSnapshot",
            snapshot.PublicId,
            userId,
            $"Agreement snapshot '{snapshot.Title}' marked ready for approval.",
            cancellationToken);

        return (await snapshotService.GetSnapshotAsync(tenantId, workspacePublicId, snapshotPublicId, cancellationToken), null);
    }

    public async Task<(ReviewInvitationDetail? Invitation, string? Error)> CreateInvitationAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        Guid userId,
        CreateReviewInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await ResolveSnapshotAsync(tenantId, workspacePublicId, snapshotPublicId, cancellationToken);
        if (snapshot is null)
        {
            return (null, null);
        }

        if (snapshot.Status is SnapshotStatus.Draft or SnapshotStatus.Approved)
        {
            return (null, "Invitations can only be sent for snapshots in an active review state.");
        }

        var capabilityCheck = await entitlementService.CheckCapabilityAsync(
            tenantId,
            Capability.CanInviteExternalReviewer,
            cancellationToken);

        if (!capabilityCheck.IsAllowed)
        {
            return (null, capabilityCheck.DenialReason ?? "External reviewer invitation limit reached.");
        }

        var email = request.ReviewerEmail.Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return (null, "A valid reviewer email is required.");
        }

        try
        {
            await entitlementService.RecordUsageAsync(
                tenantId,
                UsageMetric.ExternalInvitationsSentThisMonth,
                increment: 1,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }

        var expirationDays = request.ExpirationDays is > 0 and <= 30
            ? request.ExpirationDays.Value
            : DefaultInvitationExpirationDays;

        var now = DateTime.UtcNow;
        var invitation = new ReviewInvitation
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            AgreementSnapshotId = snapshot.Id,
            Token = Guid.NewGuid(),
            ReviewerEmail = email,
            ReviewerName = request.ReviewerName?.Trim(),
            Status = InvitationStatus.Active,
            ExpiresAtUtc = now.AddDays(expirationDays),
            CreatedAtUtc = now,
            CreatedByUserId = userId
        };

        dbContext.ReviewInvitations.Add(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.ReviewInvitationSent,
            "ReviewInvitation",
            invitation.PublicId,
            userId,
            $"Review invitation sent to external reviewer for snapshot '{snapshot.Title}'.",
            cancellationToken);

        return (MapInvitationDetail(invitation), null);
    }

    public async Task<(bool Success, string? Error)> RevokeInvitationAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        Guid invitationPublicId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await ResolveSnapshotAsync(tenantId, workspacePublicId, snapshotPublicId, cancellationToken);
        if (snapshot is null)
        {
            return (false, null);
        }

        var invitation = await dbContext.ReviewInvitations
            .SingleOrDefaultAsync(
                i => i.TenantId == tenantId &&
                     i.AgreementSnapshotId == snapshot.Id &&
                     i.PublicId == invitationPublicId,
                cancellationToken);

        if (invitation is null)
        {
            return (false, null);
        }

        if (invitation.Status == InvitationStatus.Revoked)
        {
            return (false, "Invitation is already revoked.");
        }

        var now = DateTime.UtcNow;
        invitation.Status = InvitationStatus.Revoked;
        invitation.RevokedAtUtc = now;
        invitation.RevokedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.ReviewInvitationRevoked,
            "ReviewInvitation",
            invitation.PublicId,
            userId,
            "Review invitation revoked.",
            cancellationToken);

        return (true, null);
    }

    public async Task<IReadOnlyList<ReviewInvitationSummary>?> ListInvitationsAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await ResolveSnapshotAsync(tenantId, workspacePublicId, snapshotPublicId, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var invitations = await dbContext.ReviewInvitations
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.AgreementSnapshotId == snapshot.Id)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return invitations
            .Select(i => new ReviewInvitationSummary(
                i.PublicId,
                i.ReviewerEmail,
                i.ReviewerName,
                ResolveInvitationStatus(i, now),
                i.ExpiresAtUtc,
                i.CreatedAtUtc,
                i.RevokedAtUtc))
            .ToList();
    }

    public async Task<ApprovalRecordDetail?> GetApprovalRecordAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await ResolveSnapshotAsync(tenantId, workspacePublicId, snapshotPublicId, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        var approval = await dbContext.ApprovalRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                a => a.TenantId == tenantId && a.AgreementSnapshotId == snapshot.Id,
                cancellationToken);

        return approval is null ? null : MapApprovalDetail(approval);
    }

    public async Task<ExternalReviewSnapshotDetail?> GetSnapshotForReviewAsync(
        Guid token,
        CancellationToken cancellationToken = default)
    {
        var invitation = await ResolveActiveInvitationByTokenAsync(token, cancellationToken);
        if (invitation is null)
        {
            return null;
        }

        var snapshot = await LoadSnapshotWithSectionsAsync(invitation.TenantId, invitation.AgreementSnapshotId, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        if (snapshot.Status is SnapshotStatus.Draft or SnapshotStatus.Approved)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        invitation.LastAccessedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        var comments = await dbContext.ReviewComments
            .AsNoTracking()
            .Where(c => c.TenantId == invitation.TenantId && c.AgreementSnapshotId == snapshot.Id)
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => new ReviewCommentDetail(c.PublicId, c.AuthorName, c.Content, c.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var suggestions = await dbContext.ChangeSuggestions
            .AsNoTracking()
            .Where(c => c.TenantId == invitation.TenantId && c.AgreementSnapshotId == snapshot.Id)
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => new ChangeSuggestionDetail(
                c.PublicId,
                c.AuthorName,
                c.SectionReference,
                c.SuggestedChange,
                c.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new ExternalReviewSnapshotDetail(MapSnapshotDetail(snapshot), comments, suggestions);
    }

    public async Task<(ReviewCommentDetail? Comment, string? Error)> AddCommentAsync(
        Guid token,
        AddReviewCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var invitation = await ResolveActiveInvitationByTokenAsync(token, cancellationToken);
        if (invitation is null)
        {
            return (null, null);
        }

        var snapshot = await dbContext.AgreementSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.Id == invitation.AgreementSnapshotId && s.TenantId == invitation.TenantId,
                cancellationToken);

        if (snapshot is null || snapshot.Status is SnapshotStatus.Draft or SnapshotStatus.Approved)
        {
            return (null, "Comments cannot be added to this snapshot.");
        }

        var authorName = request.AuthorName.Trim();
        var content = request.Content.Trim();
        if (string.IsNullOrWhiteSpace(authorName) || string.IsNullOrWhiteSpace(content))
        {
            return (null, "Author name and comment content are required.");
        }

        var now = DateTime.UtcNow;
        var comment = new ReviewComment
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = invitation.TenantId,
            AgreementSnapshotId = snapshot.Id,
            ReviewInvitationId = invitation.Id,
            AuthorName = authorName,
            Content = content,
            CreatedAtUtc = now
        };

        dbContext.ReviewComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            invitation.TenantId,
            AuditEventType.ReviewCommentAdded,
            "ReviewComment",
            comment.PublicId,
            null,
            "External reviewer comment added.",
            cancellationToken);

        return (new ReviewCommentDetail(comment.PublicId, comment.AuthorName, comment.Content, comment.CreatedAtUtc), null);
    }

    public async Task<(ChangeSuggestionDetail? Suggestion, string? Error)> AddChangeSuggestionAsync(
        Guid token,
        AddChangeSuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var invitation = await ResolveActiveInvitationByTokenAsync(token, cancellationToken);
        if (invitation is null)
        {
            return (null, null);
        }

        var snapshot = await dbContext.AgreementSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.Id == invitation.AgreementSnapshotId && s.TenantId == invitation.TenantId,
                cancellationToken);

        if (snapshot is null || snapshot.Status is SnapshotStatus.Draft or SnapshotStatus.Approved)
        {
            return (null, "Change suggestions cannot be added to this snapshot.");
        }

        var authorName = request.AuthorName.Trim();
        var sectionReference = request.SectionReference.Trim();
        var suggestedChange = request.SuggestedChange.Trim();
        if (string.IsNullOrWhiteSpace(authorName) ||
            string.IsNullOrWhiteSpace(sectionReference) ||
            string.IsNullOrWhiteSpace(suggestedChange))
        {
            return (null, "Author name, section reference, and suggested change are required.");
        }

        var now = DateTime.UtcNow;
        var suggestion = new ChangeSuggestion
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = invitation.TenantId,
            AgreementSnapshotId = snapshot.Id,
            ReviewInvitationId = invitation.Id,
            AuthorName = authorName,
            SectionReference = sectionReference,
            SuggestedChange = suggestedChange,
            CreatedAtUtc = now
        };

        dbContext.ChangeSuggestions.Add(suggestion);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            invitation.TenantId,
            AuditEventType.ChangeSuggestionAdded,
            "ChangeSuggestion",
            suggestion.PublicId,
            null,
            "External reviewer change suggestion added.",
            cancellationToken);

        return (new ChangeSuggestionDetail(
            suggestion.PublicId,
            suggestion.AuthorName,
            suggestion.SectionReference,
            suggestion.SuggestedChange,
            suggestion.CreatedAtUtc), null);
    }

    public async Task<(AgreementSnapshotDetail? Snapshot, string? Error)> RequestChangesAsync(
        Guid token,
        CancellationToken cancellationToken = default)
    {
        var invitation = await ResolveActiveInvitationByTokenAsync(token, cancellationToken);
        if (invitation is null)
        {
            return (null, null);
        }

        var snapshot = await dbContext.AgreementSnapshots
            .SingleOrDefaultAsync(
                s => s.Id == invitation.AgreementSnapshotId && s.TenantId == invitation.TenantId,
                cancellationToken);

        if (snapshot is null)
        {
            return (null, null);
        }

        if (snapshot.Status is not (SnapshotStatus.Shared or SnapshotStatus.ReadyForApproval))
        {
            return (null, "Changes can only be requested for snapshots shared or ready for approval.");
        }

        var now = DateTime.UtcNow;
        snapshot.Status = SnapshotStatus.ChangesRequested;
        snapshot.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            invitation.TenantId,
            AuditEventType.SnapshotChangesRequested,
            "AgreementSnapshot",
            snapshot.PublicId,
            null,
            "External reviewer requested changes.",
            cancellationToken);

        var workspacePublicId = await dbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.Id == snapshot.WorkspaceId && w.TenantId == snapshot.TenantId)
            .Select(w => w.PublicId)
            .SingleAsync(cancellationToken);

        return (await snapshotService.GetSnapshotAsync(invitation.TenantId, workspacePublicId, snapshot.PublicId, cancellationToken), null);
    }

    public async Task<(ApprovalRecordDetail? Approval, string? Error)> ApproveSnapshotAsync(
        Guid token,
        ApproveSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        var invitation = await ResolveActiveInvitationByTokenAsync(token, cancellationToken);
        if (invitation is null)
        {
            return (null, null);
        }

        var snapshot = await LoadSnapshotWithSectionsAsync(invitation.TenantId, invitation.AgreementSnapshotId, cancellationToken);
        if (snapshot is null)
        {
            return (null, null);
        }

        if (snapshot.Status != SnapshotStatus.ReadyForApproval)
        {
            return (null, "Snapshot must be marked ready for approval before it can be approved.");
        }

        var existingApproval = await dbContext.ApprovalRecords
            .AnyAsync(a => a.TenantId == invitation.TenantId && a.AgreementSnapshotId == snapshot.Id, cancellationToken);

        if (existingApproval)
        {
            return (null, "This snapshot has already been approved.");
        }

        var approverName = request.ApproverName.Trim();
        var approverEmail = request.ApproverEmail.Trim();
        var confirmation = request.ConfirmationStatement.Trim();

        if (string.IsNullOrWhiteSpace(approverName) ||
            string.IsNullOrWhiteSpace(approverEmail) ||
            !approverEmail.Contains('@'))
        {
            return (null, "Approver name and valid email are required.");
        }

        if (confirmation.Length < MinConfirmationLength)
        {
            return (null, $"Confirmation statement must be at least {MinConfirmationLength} characters.");
        }

        var canonicalHash = CanonicalSnapshotHasher.ComputeSha256Hex(snapshot);
        var now = DateTime.UtcNow;

        var approval = new ApprovalRecord
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = invitation.TenantId,
            AgreementSnapshotId = snapshot.Id,
            ReviewInvitationId = invitation.Id,
            ApproverName = approverName,
            ApproverEmail = approverEmail,
            CanonicalHashSha256 = canonicalHash,
            ConfirmationStatement = confirmation,
            SnapshotVersionNumber = snapshot.VersionNumber,
            ApprovedAtUtc = now
        };

        snapshot.Status = SnapshotStatus.Approved;
        snapshot.CanonicalHashSha256 = canonicalHash;
        snapshot.ApprovedAtUtc = now;
        snapshot.UpdatedAtUtc = now;

        if (snapshot.SourceSnapshotId is not null)
        {
            var sourceSnapshot = await dbContext.AgreementSnapshots
                .SingleOrDefaultAsync(
                    s => s.TenantId == invitation.TenantId && s.Id == snapshot.SourceSnapshotId,
                    cancellationToken);

            if (sourceSnapshot is not null && sourceSnapshot.Status == SnapshotStatus.Approved)
            {
                sourceSnapshot.Status = SnapshotStatus.Superseded;
                sourceSnapshot.UpdatedAtUtc = now;
            }
        }

        if (snapshot.ChangeRequestId is not null)
        {
            await ChangeLedgerService.MarkChangeRequestImplementedAsync(
                dbContext,
                auditService,
                invitation.TenantId,
                snapshot.ChangeRequestId.Value,
                cancellationToken);
        }

        dbContext.ApprovalRecords.Add(approval);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            invitation.TenantId,
            AuditEventType.SnapshotApproved,
            "ApprovalRecord",
            approval.PublicId,
            null,
            $"Agreement snapshot approved with integrity hash recorded.",
            cancellationToken);

        return (MapApprovalDetail(approval), null);
    }

    private async Task<AgreementSnapshot?> ResolveSnapshotAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        CancellationToken cancellationToken)
    {
        var workspaceId = await dbContext.Workspaces
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId && w.PublicId == workspacePublicId)
            .Select(w => (Guid?)w.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (workspaceId is null)
        {
            return null;
        }

        return await dbContext.AgreementSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.TenantId == tenantId &&
                     s.WorkspaceId == workspaceId &&
                     s.PublicId == snapshotPublicId,
                cancellationToken);
    }

    private async Task<ReviewInvitation?> ResolveActiveInvitationByTokenAsync(
        Guid token,
        CancellationToken cancellationToken)
    {
        var invitation = await dbContext.ReviewInvitations
            .SingleOrDefaultAsync(i => i.Token == token, cancellationToken);

        if (invitation is null)
        {
            return null;
        }

        if (invitation.Status == InvitationStatus.Revoked)
        {
            return null;
        }

        if (invitation.ExpiresAtUtc <= DateTime.UtcNow)
        {
            if (invitation.Status == InvitationStatus.Active)
            {
                invitation.Status = InvitationStatus.Expired;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        return invitation;
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

    private static ReviewInvitationDetail MapInvitationDetail(ReviewInvitation invitation) =>
        new(
            invitation.PublicId,
            invitation.Token,
            invitation.ReviewerEmail,
            invitation.ReviewerName,
            InvitationStatusDto.Active,
            invitation.ExpiresAtUtc,
            invitation.CreatedAtUtc,
            $"/api/v1/external/review/{invitation.Token}");

    private static ApprovalRecordDetail MapApprovalDetail(ApprovalRecord approval) =>
        new(
            approval.PublicId,
            approval.ApproverName,
            approval.ApproverEmail,
            approval.CanonicalHashSha256,
            approval.ConfirmationStatement,
            approval.SnapshotVersionNumber,
            approval.ApprovedAtUtc);

    private static InvitationStatusDto ResolveInvitationStatus(ReviewInvitation invitation, DateTime now)
    {
        if (invitation.Status == InvitationStatus.Revoked)
        {
            return InvitationStatusDto.Revoked;
        }

        if (invitation.ExpiresAtUtc <= now)
        {
            return InvitationStatusDto.Expired;
        }

        return InvitationStatusDto.Active;
    }

    private static AgreementSnapshotDetail MapSnapshotDetail(AgreementSnapshot snapshot) =>
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
}
