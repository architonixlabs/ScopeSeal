using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScopeSeal.AgreementSnapshots.Domain;
using ScopeSeal.AgreementSnapshots.Services;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Audit.Services;
using ScopeSeal.Documents.Domain;
using ScopeSeal.Documents.Services;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Extraction.Domain;
using ScopeSeal.Extraction.Services;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Infrastructure.Services;

public sealed class ExtractionService(
    ApplicationDbContext dbContext,
    IEntitlementService entitlementService,
    IAuditService auditService,
    IAgreementSnapshotService snapshotService,
    AiExtractionProviderFactory providerFactory,
    IOptions<ScopeSealOptions> scopeSealOptions) : IExtractionService
{
    public async Task<(ExtractionRunDetail? Run, string? Error)> CreateExtractionRunAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid documentPublicId,
        Guid userId,
        CreateExtractionRunRequest request,
        CancellationToken cancellationToken = default)
    {
        if (scopeSealOptions.Value.Ai.KillSwitchEnabled)
        {
            return (null, "AI extraction is temporarily disabled by an administrator.");
        }

        var mode = providerFactory.CurrentMode;
        if (string.Equals(mode, "ManualOnly", StringComparison.Ordinal))
        {
            return (null, "AI extraction is disabled. Manual snapshot editing remains available.");
        }

        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return (null, null);
        }

        var document = await dbContext.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                d => d.TenantId == tenantId &&
                     d.WorkspaceId == workspaceId &&
                     d.PublicId == documentPublicId,
                cancellationToken);

        if (document is null)
        {
            return (null, null);
        }

        if (document.Status != DocumentStatus.Available)
        {
            return (null, "Document must be available before extraction can run.");
        }

        Guid? snapshotId = null;
        if (request.SnapshotPublicId is Guid snapshotPublicId)
        {
            var snapshot = await dbContext.AgreementSnapshots
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    s => s.TenantId == tenantId &&
                         s.WorkspaceId == workspaceId &&
                         s.PublicId == snapshotPublicId,
                    cancellationToken);

            if (snapshot is null)
            {
                return (null, null);
            }

            if (snapshot.Status != SnapshotStatus.Draft)
            {
                return (null, "Extraction can only target draft snapshots.");
            }

            snapshotId = snapshot.Id;
        }

        var capabilityCheck = await entitlementService.CheckCapabilityAsync(
            tenantId,
            Capability.CanUseAiExtraction,
            cancellationToken);

        if (!capabilityCheck.IsAllowed)
        {
            return (null, capabilityCheck.DenialReason ?? "AI extraction is not available for this plan.");
        }

        var latestVersion = await dbContext.DocumentVersions
            .AsNoTracking()
            .Where(v => v.DocumentId == document.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestVersion is null)
        {
            return (null, "Document version not found.");
        }

        var now = DateTime.UtcNow;
        var processingJob = new ProcessingJob
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentVersionId = latestVersion.Id,
            JobType = ProcessingJobType.TextExtraction,
            Status = ProcessingJobStatus.Pending,
            CreatedAtUtc = now
        };

        var run = new ExtractionRun
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = workspaceId.Value,
            DocumentId = document.Id,
            AgreementSnapshotId = snapshotId,
            ProcessingJobId = processingJob.Id,
            Status = ExtractionRunStatus.Pending,
            AiMode = mode,
            CreatedAtUtc = now,
            CreatedByUserId = userId
        };

        dbContext.ProcessingJobs.Add(processingJob);
        dbContext.ExtractionRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.ExtractionRunStarted,
            "ExtractionRun",
            run.PublicId,
            userId,
            $"AI extraction run queued for document '{document.OriginalFileName}'.",
            cancellationToken);

        return (await MapRunDetailAsync(tenantId, workspacePublicId, run.PublicId, cancellationToken), null);
    }

    public async Task<ExtractionRunDetail?> GetExtractionRunAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid extractionRunPublicId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return null;
        }

        var run = await dbContext.ExtractionRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.TenantId == tenantId &&
                     r.WorkspaceId == workspaceId &&
                     r.PublicId == extractionRunPublicId,
                cancellationToken);

        return run is null
            ? null
            : await MapRunDetailAsync(tenantId, workspacePublicId, run.PublicId, cancellationToken);
    }

    public async Task<(ExtractedFactDetail? Fact, string? Error)> ReviewFactAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid extractionRunPublicId,
        Guid factPublicId,
        Guid userId,
        ReviewExtractedFactRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ReviewStatus is FactReviewStatus.Draft)
        {
            return (null, "Review status must be Accepted, Rejected, or Uncertain.");
        }

        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return (null, null);
        }

        var run = await dbContext.ExtractionRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.TenantId == tenantId &&
                     r.WorkspaceId == workspaceId &&
                     r.PublicId == extractionRunPublicId,
                cancellationToken);

        if (run is null)
        {
            return (null, null);
        }

        if (run.Status != ExtractionRunStatus.Completed)
        {
            return (null, "Facts can only be reviewed after extraction completes.");
        }

        var fact = await dbContext.ExtractedFacts
            .SingleOrDefaultAsync(
                f => f.TenantId == tenantId &&
                     f.ExtractionRunId == run.Id &&
                     f.PublicId == factPublicId,
                cancellationToken);

        if (fact is null)
        {
            return (null, null);
        }

        var now = DateTime.UtcNow;
        fact.ReviewStatus = request.ReviewStatus;
        fact.ReviewedAtUtc = now;
        fact.ReviewedByUserId = userId;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.ExtractionFactReviewed,
            "ExtractedFact",
            fact.PublicId,
            userId,
            $"Extracted fact '{fact.Title}' marked {request.ReviewStatus}.",
            cancellationToken);

        return (MapFactDetail(fact), null);
    }

    public async Task<(ApplyExtractionResult? Result, string? Error)> ApplyAcceptedFactsAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid extractionRunPublicId,
        Guid snapshotPublicId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return (null, null);
        }

        var run = await dbContext.ExtractionRuns
            .Include(r => r.Facts)
            .SingleOrDefaultAsync(
                r => r.TenantId == tenantId &&
                     r.WorkspaceId == workspaceId &&
                     r.PublicId == extractionRunPublicId,
                cancellationToken);

        if (run is null)
        {
            return (null, null);
        }

        if (run.Status != ExtractionRunStatus.Completed)
        {
            return (null, "Accepted facts can only be applied after extraction completes.");
        }

        var snapshot = await dbContext.AgreementSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.TenantId == tenantId &&
                     s.WorkspaceId == workspaceId &&
                     s.PublicId == snapshotPublicId,
                cancellationToken);

        if (snapshot is null)
        {
            return (null, null);
        }

        if (snapshot.Status != SnapshotStatus.Draft)
        {
            return (null, "Accepted facts can only be applied to draft snapshots.");
        }

        if (run.AgreementSnapshotId is not null && run.AgreementSnapshotId != snapshot.Id)
        {
            return (null, "Extraction run is linked to a different snapshot.");
        }

        var acceptedFacts = run.Facts
            .Where(f => f.ReviewStatus == FactReviewStatus.Accepted)
            .ToList();

        if (acceptedFacts.Count == 0)
        {
            return (null, "No accepted facts are available to apply.");
        }

        var currentSnapshot = await snapshotService.GetSnapshotAsync(
            tenantId,
            workspacePublicId,
            snapshotPublicId,
            cancellationToken);

        if (currentSnapshot is null)
        {
            return (null, null);
        }

        var scopeItems = currentSnapshot.ScopeItems.Select(i => new SectionItemInput(
            i.PublicId, i.SortOrder, i.Title, i.Description)).ToList();
        var exclusions = currentSnapshot.Exclusions.Select(i => new SectionItemInput(
            i.PublicId, i.SortOrder, i.Title, i.Description)).ToList();
        var deliverables = currentSnapshot.Deliverables.Select(i => new SectionItemInput(
            i.PublicId, i.SortOrder, i.Title, i.Description)).ToList();
        var commitments = currentSnapshot.Commitments.Select(i => new SectionItemInput(
            i.PublicId, i.SortOrder, i.Title, i.Description)).ToList();
        var paymentMilestones = currentSnapshot.PaymentMilestones.Select(i => new PaymentMilestoneInput(
            i.PublicId, i.SortOrder, i.Title, i.Description, i.AmountMinorUnits, i.CurrencyCode, i.DueDateUtc)).ToList();
        var timelineMilestones = currentSnapshot.TimelineMilestones.Select(i => new TimelineMilestoneInput(
            i.PublicId, i.SortOrder, i.Title, i.Description, i.TargetDateUtc)).ToList();
        var assumptions = currentSnapshot.Assumptions.Select(i => new SectionItemInput(
            i.PublicId, i.SortOrder, i.Title, i.Description)).ToList();
        var openQuestions = currentSnapshot.OpenQuestions.Select(i => new SectionItemInput(
            i.PublicId, i.SortOrder, i.Title, i.Description)).ToList();

        var nextSortOrder = scopeItems.Count + exclusions.Count + deliverables.Count + 1;
        foreach (var fact in acceptedFacts)
        {
            switch (fact.SectionType)
            {
                case ExtractedFactSectionType.ScopeItem:
                    scopeItems.Add(new SectionItemInput(null, nextSortOrder++, fact.Title, fact.Description));
                    break;
                case ExtractedFactSectionType.Exclusion:
                    exclusions.Add(new SectionItemInput(null, nextSortOrder++, fact.Title, fact.Description));
                    break;
                case ExtractedFactSectionType.Deliverable:
                    deliverables.Add(new SectionItemInput(null, nextSortOrder++, fact.Title, fact.Description));
                    break;
                case ExtractedFactSectionType.Commitment:
                    commitments.Add(new SectionItemInput(null, nextSortOrder++, fact.Title, fact.Description));
                    break;
                case ExtractedFactSectionType.PaymentMilestone:
                    paymentMilestones.Add(new PaymentMilestoneInput(
                        null,
                        nextSortOrder++,
                        fact.Title,
                        fact.Description,
                        fact.AmountMinorUnits,
                        fact.CurrencyCode,
                        null));
                    break;
                case ExtractedFactSectionType.TimelineMilestone:
                    timelineMilestones.Add(new TimelineMilestoneInput(
                        null,
                        nextSortOrder++,
                        fact.Title,
                        fact.Description,
                        null));
                    break;
                case ExtractedFactSectionType.Assumption:
                    assumptions.Add(new SectionItemInput(null, nextSortOrder++, fact.Title, fact.Description));
                    break;
                case ExtractedFactSectionType.OpenQuestion:
                    openQuestions.Add(new SectionItemInput(null, nextSortOrder++, fact.Title, fact.Description));
                    break;
            }
        }

        var updateRequest = new UpdateAgreementSnapshotRequest(
            currentSnapshot.Title,
            currentSnapshot.Description,
            currentSnapshot.UpdatedAtUtc,
            scopeItems,
            exclusions,
            deliverables,
            commitments,
            paymentMilestones,
            timelineMilestones,
            [],
            assumptions,
            openQuestions);

        var (updatedSnapshot, updateError, concurrencyConflict) = await snapshotService.UpdateSnapshotAsync(
            tenantId,
            workspacePublicId,
            snapshotPublicId,
            userId,
            updateRequest,
            cancellationToken);

        if (updateError is not null || updatedSnapshot is null)
        {
            return (null, updateError ?? "Unable to apply accepted facts.");
        }

        if (concurrencyConflict)
        {
            return (null, "Snapshot was updated concurrently. Refresh and try again.");
        }

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.ExtractionFactsApplied,
            "ExtractionRun",
            run.PublicId,
            userId,
            $"Applied {acceptedFacts.Count} accepted extracted facts to snapshot '{updatedSnapshot.Title}'.",
            cancellationToken);

        var runDetail = await MapRunDetailAsync(tenantId, workspacePublicId, run.PublicId, cancellationToken);
        return runDetail is null
            ? (null, "Extraction run not found after apply.")
            : (new ApplyExtractionResult(runDetail, updatedSnapshot), null);
    }

    private async Task<ExtractionRunDetail?> MapRunDetailAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid extractionRunPublicId,
        CancellationToken cancellationToken)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return null;
        }

        var run = await dbContext.ExtractionRuns
            .AsNoTracking()
            .Include(r => r.Facts)
            .SingleOrDefaultAsync(
                r => r.TenantId == tenantId &&
                     r.WorkspaceId == workspaceId &&
                     r.PublicId == extractionRunPublicId,
                cancellationToken);

        if (run is null)
        {
            return null;
        }

        var documentPublicId = await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.Id == run.DocumentId)
            .Select(d => d.PublicId)
            .SingleAsync(cancellationToken);

        Guid? snapshotPublicId = null;
        if (run.AgreementSnapshotId is Guid snapshotId)
        {
            snapshotPublicId = await dbContext.AgreementSnapshots
                .AsNoTracking()
                .Where(s => s.Id == snapshotId)
                .Select(s => s.PublicId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return new ExtractionRunDetail(
            run.PublicId,
            documentPublicId,
            snapshotPublicId,
            run.Status,
            run.AiMode,
            run.Facts.OrderBy(f => f.CreatedAtUtc).Select(MapFactDetail).ToArray(),
            run.CreatedAtUtc,
            run.CompletedAtUtc,
            run.ErrorMessage);
    }

    private static ExtractedFactDetail MapFactDetail(ExtractedFact fact) => new(
        fact.PublicId,
        fact.SectionType,
        fact.Title,
        fact.Description,
        fact.AmountMinorUnits,
        fact.CurrencyCode,
        fact.ConfidenceScore,
        fact.ReviewStatus,
        fact.SourceDocumentName,
        fact.SourceHashValue,
        fact.SourcePageNumber,
        fact.SourceExcerpt,
        fact.CreatedAtUtc,
        fact.ReviewedAtUtc);

    private async Task<Guid?> ResolveWorkspaceIdAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken)
    {
        var workspace = await dbContext.Workspaces
            .AsNoTracking()
            .SingleOrDefaultAsync(
                w => w.TenantId == tenantId && w.PublicId == workspacePublicId,
                cancellationToken);

        return workspace?.Id;
    }
}
