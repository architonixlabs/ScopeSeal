using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScopeSeal.Administration.Configuration;
using ScopeSeal.Administration.Domain;
using ScopeSeal.Administration.Services;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Billing.Domain;
using ScopeSeal.Documents.Domain;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Extraction.Domain;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Privacy.Domain;

namespace ScopeSeal.Infrastructure.Services;

public sealed class AdministrationService(
    ApplicationDbContext dbContext,
    IEntitlementService entitlementService,
    IOptions<AdministrationOptions> administrationOptions) : IAdministrationService
{
    private static readonly AuditEventType[] BillingAuditTypes =
    [
        AuditEventType.BillingCheckoutCreated,
        AuditEventType.BillingPaymentVerified,
        AuditEventType.BillingWebhookProcessed,
        AuditEventType.BillingEntitlementGranted,
        AuditEventType.BillingSubscriptionCancelled
    ];

    public async Task<IReadOnlyList<TenantMetadataSummary>> SearchTenantsAsync(
        string? query,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var maxResults = Math.Clamp(
            limit ?? administrationOptions.Value.TenantSearchMaxResults,
            1,
            100);

        var tenantsQuery = dbContext.Tenants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim();
            if (Guid.TryParse(normalized, out var publicId))
            {
                tenantsQuery = tenantsQuery.Where(t => t.PublicId == publicId);
            }
            else
            {
                tenantsQuery = tenantsQuery.Where(t => EF.Functions.ILike(t.Name, $"%{normalized}%"));
            }
        }

        var tenants = await tenantsQuery
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(maxResults)
            .Select(t => new
            {
                t.Id,
                t.PublicId,
                t.Name,
                t.CreatedAtUtc,
                MemberCount = t.Members.Count
            })
            .ToListAsync(cancellationToken);

        var summaries = new List<TenantMetadataSummary>(tenants.Count);
        foreach (var tenant in tenants)
        {
            var entitlement = await entitlementService.GetSummaryAsync(
                tenant.Id,
                cancellationToken);

            summaries.Add(new TenantMetadataSummary(
                tenant.PublicId,
                tenant.Name,
                tenant.CreatedAtUtc,
                tenant.MemberCount,
                entitlement?.PlanCode.ToString() ?? PlanCode.Free.ToString()));
        }

        return summaries;
    }

    public async Task<TenantInspectionSummary?> GetTenantInspectionAsync(
        Guid tenantPublicId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.PublicId == tenantPublicId, cancellationToken);

        if (tenant is null)
        {
            return null;
        }

        var entitlement = await entitlementService.GetSummaryAsync(tenant.Id, cancellationToken);
        var activeWorkspaceCount = await dbContext.Workspaces
            .AsNoTracking()
            .CountAsync(w => w.TenantId == tenant.Id && w.Status == Workspaces.Domain.WorkspaceStatus.Active, cancellationToken);

        var openPrivacyRequestCount = await dbContext.PrivacyRequests
            .AsNoTracking()
            .CountAsync(
                r => r.TenantId == tenant.Id &&
                     r.Status != PrivacyRequestStatus.Completed &&
                     r.Status != PrivacyRequestStatus.Rejected,
                cancellationToken);

        var subscription = await dbContext.TenantSubscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenant.Id)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        SubscriptionInspectionSummary? subscriptionSummary = subscription is null
            ? null
            : new SubscriptionInspectionSummary(
                subscription.PublicId,
                subscription.PlanCode.ToString(),
                subscription.Interval.ToString(),
                subscription.Status.ToString(),
                subscription.EntitlementGranted,
                subscription.GracePeriodEndsAtUtc);

        return new TenantInspectionSummary(
            tenant.PublicId,
            tenant.Name,
            tenant.CreatedAtUtc,
            tenant.Members.Count,
            entitlement?.PlanCode.ToString() ?? PlanCode.Free.ToString(),
            entitlement?.Source.ToString() ?? EntitlementSource.FreePlan.ToString(),
            activeWorkspaceCount,
            openPrivacyRequestCount,
            subscriptionSummary);
    }

    public async Task<IReadOnlyList<BillingEventSummary>> ListBillingEventsAsync(
        Guid? tenantPublicId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var maxResults = Math.Clamp(limit, 1, 200);
        Guid? tenantId = null;

        if (tenantPublicId.HasValue)
        {
            tenantId = await ResolveTenantIdAsync(tenantPublicId.Value, cancellationToken);
            if (tenantId is null)
            {
                return [];
            }
        }

        var auditQuery = dbContext.AuditEvents
            .AsNoTracking()
            .Where(e => BillingAuditTypes.Contains(e.EventType));

        if (tenantId.HasValue)
        {
            auditQuery = auditQuery.Where(e => e.TenantId == tenantId.Value);
        }

        var auditEvents = await auditQuery
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

        var tenantPublicIds = await ResolveTenantPublicIdsAsync(
            auditEvents.Select(e => e.TenantId).Distinct(),
            cancellationToken);

        var auditSummaries = auditEvents.Select(e => new BillingEventSummary(
            tenantPublicIds.GetValueOrDefault(e.TenantId),
            e.EventType.ToString(),
            e.Summary ?? string.Empty,
            e.OccurredAtUtc,
            "Audit"));

        var webhookEvents = await dbContext.ProcessedWebhookEvents
            .AsNoTracking()
            .OrderByDescending(e => e.ProcessedAtUtc)
            .Take(maxResults)
            .Select(e => new BillingEventSummary(
                null,
                e.EventType,
                $"Provider event {e.ProviderEventId}",
                e.ProcessedAtUtc,
                "Webhook"))
            .ToListAsync(cancellationToken);

        return auditSummaries
            .Concat(webhookEvents)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(maxResults)
            .ToList();
    }

    public async Task<IReadOnlyList<FailedJobSummary>> ListFailedJobsAsync(
        Guid? tenantPublicId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var maxResults = Math.Clamp(limit, 1, 200);
        Guid? tenantId = null;

        if (tenantPublicId.HasValue)
        {
            tenantId = await ResolveTenantIdAsync(tenantPublicId.Value, cancellationToken);
            if (tenantId is null)
            {
                return [];
            }
        }

        var processingJobsQuery = dbContext.ProcessingJobs
            .AsNoTracking()
            .Where(j => j.Status == ProcessingJobStatus.Failed);

        if (tenantId.HasValue)
        {
            processingJobsQuery = processingJobsQuery.Where(j => j.TenantId == tenantId.Value);
        }

        var processingJobs = await processingJobsQuery
            .OrderByDescending(j => j.CreatedAtUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

        var extractionQuery = dbContext.ExtractionRuns
            .AsNoTracking()
            .Where(r => r.Status == ExtractionRunStatus.Failed);

        if (tenantId.HasValue)
        {
            extractionQuery = extractionQuery.Where(r => r.TenantId == tenantId.Value);
        }

        var extractionRuns = await extractionQuery
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

        var tenantIds = processingJobs.Select(j => j.TenantId)
            .Concat(extractionRuns.Select(r => r.TenantId))
            .Distinct()
            .ToList();

        var tenantPublicIds = await ResolveTenantPublicIdsAsync(tenantIds, cancellationToken);

        var summaries = processingJobs.Select(j => new FailedJobSummary(
            j.PublicId,
            tenantPublicIds[j.TenantId],
            j.JobType.ToString(),
            j.Status.ToString(),
            j.ErrorMessage,
            j.CreatedAtUtc)).ToList();

        summaries.AddRange(extractionRuns.Select(r => new FailedJobSummary(
            r.PublicId,
            tenantPublicIds[r.TenantId],
            "ExtractionRun",
            r.Status.ToString(),
            r.ErrorMessage,
            r.CreatedAtUtc)));

        return summaries
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(maxResults)
            .ToList();
    }

    public async Task<IReadOnlyList<DeadLetterJobSummary>> ListDeadLetterJobsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var maxResults = Math.Clamp(limit, 1, 200);

        var jobs = await dbContext.DeadLetterJobs
            .AsNoTracking()
            .OrderByDescending(j => j.FailedAtUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

        var tenantPublicIds = await ResolveTenantPublicIdsAsync(
            jobs.Select(j => j.TenantId).Distinct(),
            cancellationToken);

        return jobs.Select(j => new DeadLetterJobSummary(
            j.PublicId,
            tenantPublicIds[j.TenantId],
            j.JobCategory,
            j.SourceJobPublicId,
            j.ErrorMessage,
            j.FailedAtUtc,
            j.Status,
            j.RequeuedAtUtc)).ToList();
    }

    public async Task<int> SyncDeadLetterFromFailedJobsAsync(CancellationToken cancellationToken = default)
    {
        var failedProcessingJobs = await dbContext.ProcessingJobs
            .AsNoTracking()
            .Where(j => j.Status == ProcessingJobStatus.Failed)
            .ToListAsync(cancellationToken);

        var failedExtractionRuns = await dbContext.ExtractionRuns
            .AsNoTracking()
            .Where(r => r.Status == ExtractionRunStatus.Failed)
            .ToListAsync(cancellationToken);

        var existingSourceIds = await dbContext.DeadLetterJobs
            .AsNoTracking()
            .Select(j => j.SourceJobPublicId)
            .ToListAsync(cancellationToken);

        var existingSet = existingSourceIds.ToHashSet();
        var added = 0;

        foreach (var job in failedProcessingJobs)
        {
            if (existingSet.Contains(job.PublicId))
            {
                continue;
            }

            dbContext.DeadLetterJobs.Add(new DeadLetterJob
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = job.TenantId,
                JobCategory = job.JobType.ToString(),
                SourceJobPublicId = job.PublicId,
                ErrorMessage = job.ErrorMessage ?? "Processing job failed",
                FailedAtUtc = job.CompletedAtUtc ?? job.CreatedAtUtc,
                Status = DeadLetterStatus.Open
            });
            added++;
        }

        foreach (var run in failedExtractionRuns)
        {
            if (existingSet.Contains(run.PublicId))
            {
                continue;
            }

            dbContext.DeadLetterJobs.Add(new DeadLetterJob
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = run.TenantId,
                JobCategory = "ExtractionRun",
                SourceJobPublicId = run.PublicId,
                ErrorMessage = run.ErrorMessage ?? "Extraction run failed",
                FailedAtUtc = run.CompletedAtUtc ?? run.CreatedAtUtc,
                Status = DeadLetterStatus.Open
            });
            added++;
        }

        if (added > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return added;
    }

    public async Task<(DeadLetterJobSummary? Item, string? Error)> RequeueDeadLetterJobAsync(
        Guid deadLetterPublicId,
        CancellationToken cancellationToken = default)
    {
        var deadLetter = await dbContext.DeadLetterJobs
            .FirstOrDefaultAsync(j => j.PublicId == deadLetterPublicId, cancellationToken);

        if (deadLetter is null)
        {
            return (null, "Dead-letter job not found.");
        }

        if (deadLetter.Status == DeadLetterStatus.Requeued)
        {
            return (null, "Dead-letter job was already requeued.");
        }

        var processingJob = await dbContext.ProcessingJobs
            .FirstOrDefaultAsync(j => j.PublicId == deadLetter.SourceJobPublicId, cancellationToken);

        if (processingJob is not null)
        {
            processingJob.Status = ProcessingJobStatus.Pending;
            processingJob.ErrorMessage = null;
            processingJob.CompletedAtUtc = null;
        }
        else
        {
            var extractionRun = await dbContext.ExtractionRuns
                .FirstOrDefaultAsync(r => r.PublicId == deadLetter.SourceJobPublicId, cancellationToken);

            if (extractionRun is not null)
            {
                extractionRun.Status = ExtractionRunStatus.Pending;
                extractionRun.ErrorMessage = null;
                extractionRun.CompletedAtUtc = null;
            }
        }

        deadLetter.Status = DeadLetterStatus.Requeued;
        deadLetter.RequeuedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var tenantPublicId = await ResolveTenantPublicIdAsync(deadLetter.TenantId, cancellationToken);

        return (new DeadLetterJobSummary(
            deadLetter.PublicId,
            tenantPublicId,
            deadLetter.JobCategory,
            deadLetter.SourceJobPublicId,
            deadLetter.ErrorMessage,
            deadLetter.FailedAtUtc,
            deadLetter.Status,
            deadLetter.RequeuedAtUtc), null);
    }

    public async Task<IReadOnlyList<GrievanceQueueItemSummary>> ListGrievanceQueueAsync(
        CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.PrivacyRequests
            .AsNoTracking()
            .Where(r => r.RequestType == PrivacyRequestType.Grievance)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var tenantIds = requests.Select(r => r.TenantId).Distinct().ToList();
        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        return requests.Select(r =>
        {
            tenants.TryGetValue(r.TenantId, out var tenant);
            return new GrievanceQueueItemSummary(
                r.PublicId,
                tenant?.PublicId ?? Guid.Empty,
                tenant?.Name ?? "Unknown tenant",
                r.Subject,
                r.Status.ToString(),
                r.GrievanceCategory,
                r.CreatedAtUtc);
        }).ToList();
    }

    public async Task<IReadOnlyList<FeatureFlagSummary>> ListFeatureFlagsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlatformFeatureFlags
            .AsNoTracking()
            .OrderBy(f => f.Key)
            .Select(f => new FeatureFlagSummary(f.Key, f.IsEnabled, f.Description, f.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<(FeatureFlagSummary? Item, string? Error)> UpdateFeatureFlagAsync(
        string key,
        UpdateFeatureFlagRequest request,
        CancellationToken cancellationToken = default)
    {
        var flag = await dbContext.PlatformFeatureFlags
            .FirstOrDefaultAsync(f => f.Key == key, cancellationToken);

        if (flag is null)
        {
            return (null, "Feature flag not found.");
        }

        flag.IsEnabled = request.IsEnabled;
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            flag.Description = request.Description.Trim();
        }

        flag.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return (new FeatureFlagSummary(flag.Key, flag.IsEnabled, flag.Description, flag.UpdatedAtUtc), null);
    }

    public async Task<IReadOnlyList<NoticeVersionSummary>> ListPrivacyNoticeVersionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PrivacyNoticeVersions
            .AsNoTracking()
            .OrderByDescending(n => n.EffectiveFromUtc)
            .Select(n => new NoticeVersionSummary(
                n.PublicId,
                n.Version,
                n.Title,
                n.Summary,
                n.EffectiveFromUtc,
                n.IsCurrent))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoticeVersionSummary>> ListTermsNoticeVersionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TermsNoticeVersions
            .AsNoTracking()
            .OrderByDescending(n => n.EffectiveFromUtc)
            .Select(n => new NoticeVersionSummary(
                n.PublicId,
                n.Version,
                n.Title,
                n.Summary,
                n.EffectiveFromUtc,
                n.IsCurrent))
            .ToListAsync(cancellationToken);
    }

    public async Task<(NoticeVersionSummary? Item, string? Error)> CreateTermsNoticeVersionAsync(
        CreateNoticeVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Version) ||
            string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Summary))
        {
            return (null, "Version, title, and summary are required.");
        }

        if (request.SetAsCurrent)
        {
            var currentVersions = await dbContext.TermsNoticeVersions
                .Where(n => n.IsCurrent)
                .ToListAsync(cancellationToken);

            foreach (var current in currentVersions)
            {
                current.IsCurrent = false;
            }
        }

        var notice = new TermsNoticeVersion
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            Version = request.Version.Trim(),
            Title = request.Title.Trim(),
            Summary = request.Summary.Trim(),
            EffectiveFromUtc = request.EffectiveFromUtc,
            IsCurrent = request.SetAsCurrent,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.TermsNoticeVersions.Add(notice);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (new NoticeVersionSummary(
            notice.PublicId,
            notice.Version,
            notice.Title,
            notice.Summary,
            notice.EffectiveFromUtc,
            notice.IsCurrent), null);
    }

    public async Task<(SupportAccessGrantSummary? Item, string? Error)> CreateSupportAccessGrantAsync(
        CreateSupportAccessGrantRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.PublicId == request.TenantPublicId, cancellationToken);

        if (tenant is null)
        {
            return (null, "Tenant not found.");
        }

        if (string.IsNullOrWhiteSpace(request.OperatorReference) ||
            string.IsNullOrWhiteSpace(request.Reason))
        {
            return (null, "Operator reference and reason are required.");
        }

        var durationHours = request.DurationHours ?? administrationOptions.Value.DefaultSupportAccessHours;
        durationHours = Math.Clamp(durationHours, 1, 72);

        var grant = new SupportAccessGrant
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenant.Id,
            OperatorReference = request.OperatorReference.Trim(),
            Scope = SupportAccessScope.MetadataOnly,
            Reason = request.Reason.Trim(),
            GrantedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(durationHours)
        };

        dbContext.SupportAccessGrants.Add(grant);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (new SupportAccessGrantSummary(
            grant.PublicId,
            tenant.PublicId,
            tenant.Name,
            grant.Scope,
            grant.OperatorReference,
            grant.Reason,
            grant.GrantedAtUtc,
            grant.ExpiresAtUtc,
            grant.RevokedAtUtc,
            IsGrantActive(grant)), null);
    }

    public async Task<IReadOnlyList<SupportAccessGrantSummary>> ListSupportAccessGrantsAsync(
        Guid? tenantPublicId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SupportAccessGrants.AsNoTracking();

        if (tenantPublicId.HasValue)
        {
            var tenantId = await ResolveTenantIdAsync(tenantPublicId.Value, cancellationToken);
            if (tenantId is null)
            {
                return [];
            }

            query = query.Where(g => g.TenantId == tenantId.Value);
        }

        var grants = await query
            .OrderByDescending(g => g.GrantedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var tenantIds = grants.Select(g => g.TenantId).Distinct().ToList();
        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        return grants.Select(g =>
        {
            tenants.TryGetValue(g.TenantId, out var tenant);
            return new SupportAccessGrantSummary(
                g.PublicId,
                tenant?.PublicId ?? Guid.Empty,
                tenant?.Name ?? "Unknown tenant",
                g.Scope,
                g.OperatorReference,
                g.Reason,
                g.GrantedAtUtc,
                g.ExpiresAtUtc,
                g.RevokedAtUtc,
                IsGrantActive(g));
        }).ToList();
    }

    public async Task<(SupportAccessGrantSummary? Item, string? Error)> RevokeSupportAccessGrantAsync(
        Guid grantPublicId,
        RevokeSupportAccessGrantRequest request,
        CancellationToken cancellationToken = default)
    {
        var grant = await dbContext.SupportAccessGrants
            .FirstOrDefaultAsync(g => g.PublicId == grantPublicId, cancellationToken);

        if (grant is null)
        {
            return (null, "Support access grant not found.");
        }

        if (grant.RevokedAtUtc.HasValue)
        {
            return (null, "Support access grant is already revoked.");
        }

        grant.RevokedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            grant.Reason = $"{grant.Reason} | Revoked: {request.Reason.Trim()}";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstAsync(t => t.Id == grant.TenantId, cancellationToken);

        return (new SupportAccessGrantSummary(
            grant.PublicId,
            tenant.PublicId,
            tenant.Name,
            grant.Scope,
            grant.OperatorReference,
            grant.Reason,
            grant.GrantedAtUtc,
            grant.ExpiresAtUtc,
            grant.RevokedAtUtc,
            IsGrantActive(grant)), null);
    }

    public async Task<IReadOnlyList<AuditEventSummary>> ListAuditEventsAsync(
        AuditEventQuery query,
        CancellationToken cancellationToken = default)
    {
        var maxResults = Math.Clamp(query.Limit, 1, 200);
        var eventsQuery = dbContext.AuditEvents.AsNoTracking();

        if (query.TenantPublicId.HasValue)
        {
            var tenantId = await ResolveTenantIdAsync(query.TenantPublicId.Value, cancellationToken);
            if (tenantId is null)
            {
                return [];
            }

            eventsQuery = eventsQuery.Where(e => e.TenantId == tenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.EventType) &&
            Enum.TryParse<AuditEventType>(query.EventType, true, out var eventType))
        {
            eventsQuery = eventsQuery.Where(e => e.EventType == eventType);
        }

        if (query.FromUtc.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => e.OccurredAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => e.OccurredAtUtc <= query.ToUtc.Value);
        }

        var events = await eventsQuery
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

        var tenantPublicIds = await ResolveTenantPublicIdsAsync(
            events.Select(e => e.TenantId).Distinct(),
            cancellationToken);

        return events.Select(e => new AuditEventSummary(
            tenantPublicIds[e.TenantId],
            e.EventType.ToString(),
            e.EntityType,
            e.EntityPublicId,
            e.Summary,
            e.OccurredAtUtc)).ToList();
    }

    private static bool IsGrantActive(SupportAccessGrant grant) =>
        !grant.RevokedAtUtc.HasValue && grant.ExpiresAtUtc > DateTime.UtcNow;

    private async Task<Guid?> ResolveTenantIdAsync(Guid tenantPublicId, CancellationToken cancellationToken)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.PublicId == tenantPublicId)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid> ResolveTenantPublicIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.PublicId)
            .FirstAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, Guid>> ResolveTenantPublicIdsAsync(
        IEnumerable<Guid> tenantIds,
        CancellationToken cancellationToken)
    {
        var ids = tenantIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await dbContext.Tenants
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.PublicId, cancellationToken);
    }
}
