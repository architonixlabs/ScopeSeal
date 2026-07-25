using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Audit.Services;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Privacy.Configuration;
using ScopeSeal.Privacy.Domain;
using ScopeSeal.Privacy.Services;
using ScopeSeal.Tenancy.Domain;

namespace ScopeSeal.Infrastructure.Services;

public sealed class PrivacyService(
    ApplicationDbContext dbContext,
    IEntitlementService entitlementService,
    IAuditService auditService,
    IOptions<PrivacyOptions> privacyOptions) : IPrivacyService
{
    private readonly PrivacyOptions _options = privacyOptions.Value;

    public async Task<PrivacyCentreSummaryResponse?> GetPrivacyCentreSummaryAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!await EnsurePrivacyAccessAsync(tenantId, Capability.CanAccessPrivacyCentre, cancellationToken))
        {
            return null;
        }

        var tenant = await dbContext.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        var notice = await GetCurrentNoticeAsync(cancellationToken);
        var consents = await ListConsentsAsync(tenantId, userId, cancellationToken);
        var requests = await ListRequestsAsync(tenantId, userId, cancellationToken);
        var exportJobs = await dbContext.DataExportJobs.AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.UserId == userId)
            .OrderByDescending(j => j.CreatedAtUtc)
            .Select(j => MapExportJob(j))
            .ToListAsync(cancellationToken);
        var deletionJobs = await dbContext.DeletionOrchestrationJobs.AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.UserId == userId)
            .OrderByDescending(j => j.CreatedAtUtc)
            .Select(j => MapDeletionJob(j))
            .ToListAsync(cancellationToken);
        var subprocessors = await ListSubprocessorsAsync(cancellationToken);

        return new PrivacyCentreSummaryResponse(
            tenant.PublicId,
            notice,
            consents,
            requests.Where(r => r.Status is not PrivacyRequestStatus.Completed and not PrivacyRequestStatus.Cancelled).ToList(),
            exportJobs,
            deletionJobs,
            subprocessors);
    }

    public async Task<PrivacyNoticeResponse?> GetCurrentNoticeAsync(CancellationToken cancellationToken = default)
    {
        var notice = await dbContext.PrivacyNoticeVersions.AsNoTracking()
            .Where(n => n.IsCurrent)
            .OrderByDescending(n => n.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return notice is null ? null : MapNotice(notice);
    }

    public async Task<PrivacyNoticeResponse?> GetNoticeAsync(
        Guid noticePublicId,
        CancellationToken cancellationToken = default)
    {
        var notice = await dbContext.PrivacyNoticeVersions.AsNoTracking()
            .FirstOrDefaultAsync(n => n.PublicId == noticePublicId, cancellationToken);

        return notice is null ? null : MapNotice(notice);
    }

    public async Task<(IReadOnlyList<ConsentRecordResponse> Consents, string? Error)> RecordConsentsAsync(
        Guid tenantId,
        Guid userId,
        RecordConsentsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await EnsurePrivacyAccessAsync(tenantId, Capability.CanAccessPrivacyCentre, cancellationToken))
        {
            return ([], "Privacy centre access is not available.");
        }

        if (!request.RequiredTermsAccepted)
        {
            return ([], "Required terms consent must be accepted.");
        }

        var notice = await dbContext.PrivacyNoticeVersions
            .FirstOrDefaultAsync(n => n.PublicId == request.NoticePublicId, cancellationToken);
        if (notice is null)
        {
            return ([], "Privacy notice version not found.");
        }

        var now = DateTime.UtcNow;
        var created = new List<ConsentRecord>();

        created.Add(await UpsertConsentAsync(
            tenantId,
            userId,
            notice.Id,
            ConsentType.RequiredTerms,
            "Service terms and privacy notice",
            granted: true,
            now,
            cancellationToken));

        if (request.OptionalMarketingAccepted is not null)
        {
            created.Add(await UpsertConsentAsync(
                tenantId,
                userId,
                notice.Id,
                ConsentType.OptionalMarketing,
                "Product updates and educational content",
                request.OptionalMarketingAccepted.Value,
                now,
                cancellationToken));
        }

        if (request.OptionalAnalyticsAccepted is not null)
        {
            created.Add(await UpsertConsentAsync(
                tenantId,
                userId,
                notice.Id,
                ConsentType.OptionalAnalytics,
                "Anonymous usage analytics",
                request.OptionalAnalyticsAccepted.Value,
                now,
                cancellationToken));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var consent in created)
        {
            await auditService.RecordAsync(
                tenantId,
                AuditEventType.PrivacyConsentRecorded,
                nameof(ConsentRecord),
                consent.PublicId,
                userId,
                $"Consent recorded: {consent.ConsentType} = {consent.Granted}.",
                cancellationToken);
        }

        var noticePublicId = notice.PublicId;
        return (created.Select(c => MapConsent(c, noticePublicId)).ToList(), null);
    }

    public async Task<(ConsentRecordResponse? Consent, string? Error)> WithdrawConsentAsync(
        Guid tenantId,
        Guid userId,
        Guid consentPublicId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (!await EnsurePrivacyAccessAsync(tenantId, Capability.CanAccessPrivacyCentre, cancellationToken))
        {
            return (null, "Privacy centre access is not available.");
        }

        var consent = await dbContext.ConsentRecords
            .FirstOrDefaultAsync(
                c => c.PublicId == consentPublicId && c.TenantId == tenantId && c.UserId == userId,
                cancellationToken);

        if (consent is null)
        {
            return (null, "Consent record not found.");
        }

        if (consent.ConsentType == ConsentType.RequiredTerms)
        {
            return (null, "Required terms consent cannot be withdrawn through self-service. Contact support.");
        }

        consent.Granted = false;
        consent.WithdrawnAtUtc = DateTime.UtcNow;
        consent.WithdrawalReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.PrivacyConsentWithdrawn,
            nameof(ConsentRecord),
            consent.PublicId,
            userId,
            $"Consent withdrawn: {consent.ConsentType}. Reason: {consent.WithdrawalReason ?? "none"}.",
            cancellationToken);

        var noticePublicId = await dbContext.PrivacyNoticeVersions.AsNoTracking()
            .Where(n => n.Id == consent.NoticeVersionId)
            .Select(n => n.PublicId)
            .FirstAsync(cancellationToken);

        return (MapConsent(consent, noticePublicId), null);
    }

    public async Task<IReadOnlyList<ConsentRecordResponse>> ListConsentsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var consents = await dbContext.ConsentRecords.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.UserId == userId)
            .OrderByDescending(c => c.GrantedAtUtc)
            .ToListAsync(cancellationToken);

        var noticeIds = consents.Select(c => c.NoticeVersionId).Distinct().ToList();
        var noticeMap = await dbContext.PrivacyNoticeVersions.AsNoTracking()
            .Where(n => noticeIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.PublicId, cancellationToken);

        return consents
            .Select(c => MapConsent(c, noticeMap.GetValueOrDefault(c.NoticeVersionId)))
            .ToList();
    }

    public async Task<(PrivacyRequestResponse? Request, string? Error)> SubmitRequestAsync(
        Guid tenantId,
        Guid userId,
        SubmitPrivacyRequest request,
        CancellationToken cancellationToken = default)
    {
        var capability = request.RequestType switch
        {
            PrivacyRequestType.Export => Capability.CanRequestDataExport,
            PrivacyRequestType.Erasure => Capability.CanRequestAccountDeletion,
            _ => Capability.CanAccessPrivacyCentre
        };

        if (!await EnsurePrivacyAccessAsync(tenantId, capability, cancellationToken))
        {
            return (null, "This privacy request is not available for your account.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Details))
        {
            return (null, "Subject and details are required.");
        }

        if (request.RequestType == PrivacyRequestType.Correction &&
            string.IsNullOrWhiteSpace(request.CorrectionDetails))
        {
            return (null, "Correction details are required for correction requests.");
        }

        if (request.RequestType == PrivacyRequestType.Grievance &&
            string.IsNullOrWhiteSpace(request.GrievanceCategory))
        {
            return (null, "Grievance category is required for grievance requests.");
        }

        var now = DateTime.UtcNow;
        var privacyRequest = new PrivacyRequest
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            RequestType = request.RequestType,
            Status = PrivacyRequestStatus.Submitted,
            Subject = request.Subject.Trim(),
            Details = request.Details.Trim(),
            CorrectionDetails = request.CorrectionDetails?.Trim(),
            GrievanceCategory = request.GrievanceCategory?.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.PrivacyRequests.Add(privacyRequest);

        var queueItem = new AdminPrivacyQueueItem
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            PrivacyRequestId = privacyRequest.Id,
            QueueStatus = AdminQueueStatus.Open,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        dbContext.AdminPrivacyQueueItems.Add(queueItem);

        if (request.RequestType == PrivacyRequestType.Export)
        {
            dbContext.DataExportJobs.Add(new DataExportJob
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                PrivacyRequestId = privacyRequest.Id,
                Status = ExportJobStatus.Pending,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        if (request.RequestType == PrivacyRequestType.Erasure)
        {
            dbContext.DeletionOrchestrationJobs.Add(new DeletionOrchestrationJob
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                PrivacyRequestId = privacyRequest.Id,
                Status = DeletionJobStatus.Scheduled,
                CurrentStep = DeletionStep.AccountLock,
                ScheduledBackupPurgeAtUtc = now.AddDays(_options.BackupPurgeGraceDays),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var auditType = request.RequestType switch
        {
            PrivacyRequestType.Grievance => AuditEventType.PrivacyGrievanceSubmitted,
            PrivacyRequestType.Export => AuditEventType.PrivacyExportJobCreated,
            PrivacyRequestType.Erasure => AuditEventType.PrivacyDeletionJobScheduled,
            _ => AuditEventType.PrivacyRequestSubmitted
        };

        await auditService.RecordAsync(
            tenantId,
            auditType,
            nameof(PrivacyRequest),
            privacyRequest.PublicId,
            userId,
            $"Privacy request submitted: {request.RequestType}.",
            cancellationToken);

        return (MapRequest(privacyRequest), null);
    }

    public async Task<IReadOnlyList<PrivacyRequestResponse>> ListRequestsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.PrivacyRequests.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.UserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return requests.Select(MapRequest).ToList();
    }

    public async Task<PrivacyRequestResponse?> GetRequestAsync(
        Guid tenantId,
        Guid userId,
        Guid requestPublicId,
        CancellationToken cancellationToken = default)
    {
        var request = await dbContext.PrivacyRequests.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.PublicId == requestPublicId && r.TenantId == tenantId && r.UserId == userId,
                cancellationToken);

        return request is null ? null : MapRequest(request);
    }

    public async Task<IReadOnlyList<SubprocessorResponse>> ListSubprocessorsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SubprocessorEntries.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new SubprocessorResponse(
                s.PublicId,
                s.Name,
                s.Purpose,
                s.DataProcessed,
                s.Location,
                s.ContractStatus,
                s.DpaStatus))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ProcessPendingPrivacyJobsAsync(CancellationToken cancellationToken = default)
    {
        var processed = 0;
        var now = DateTime.UtcNow;

        var pendingExports = await dbContext.DataExportJobs
            .Where(j => j.Status == ExportJobStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var job in pendingExports)
        {
            job.Status = ExportJobStatus.Ready;
            job.DownloadToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            job.ExpiresAtUtc = now.AddDays(_options.ExportLinkExpiryDays);
            job.UpdatedAtUtc = now;

            var request = await dbContext.PrivacyRequests
                .FirstOrDefaultAsync(r => r.Id == job.PrivacyRequestId, cancellationToken);
            if (request is not null)
            {
                request.Status = PrivacyRequestStatus.Processing;
                request.UpdatedAtUtc = now;
            }

            processed++;
        }

        var activeDeletions = await dbContext.DeletionOrchestrationJobs
            .Where(j =>
                j.Status == DeletionJobStatus.Scheduled ||
                j.Status == DeletionJobStatus.InProgress ||
                j.Status == DeletionJobStatus.AwaitingBackupPurge)
            .ToListAsync(cancellationToken);

        foreach (var job in activeDeletions)
        {
            if (job.CurrentStep == DeletionStep.Completed)
            {
                continue;
            }

            job.Status = DeletionJobStatus.InProgress;
            job.CurrentStep = job.CurrentStep switch
            {
                DeletionStep.AccountLock => DeletionStep.DataExportOffered,
                DeletionStep.DataExportOffered => DeletionStep.ContentAnonymization,
                DeletionStep.ContentAnonymization => DeletionStep.BlobDeletionScheduled,
                DeletionStep.BlobDeletionScheduled => DeletionStep.BackupPurgeScheduled,
                DeletionStep.BackupPurgeScheduled when now >= job.ScheduledBackupPurgeAtUtc => DeletionStep.Completed,
                DeletionStep.BackupPurgeScheduled => DeletionStep.BackupPurgeScheduled,
                _ => DeletionStep.Completed
            };
            job.UpdatedAtUtc = now;

            if (job.CurrentStep == DeletionStep.BackupPurgeScheduled && now < job.ScheduledBackupPurgeAtUtc)
            {
                job.Status = DeletionJobStatus.AwaitingBackupPurge;
            }
            else if (job.CurrentStep == DeletionStep.Completed)
            {
                job.Status = DeletionJobStatus.Completed;
                var request = await dbContext.PrivacyRequests
                    .FirstOrDefaultAsync(r => r.Id == job.PrivacyRequestId, cancellationToken);
                if (request is not null)
                {
                    request.Status = PrivacyRequestStatus.Completed;
                    request.CompletedAtUtc = now;
                    request.UpdatedAtUtc = now;
                }
            }

            processed++;
        }

        if (processed > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return processed;
    }

    public async Task<int> RunRetentionFoundationJobAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var run = new RetentionJobRun
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            JobType = "RetentionFoundationScan",
            Status = RetentionJobStatus.Running,
            StartedAtUtc = now
        };

        dbContext.RetentionJobRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        var expiredExports = await dbContext.DataExportJobs
            .Where(j => j.Status == ExportJobStatus.Ready &&
                        j.ExpiresAtUtc != null &&
                        j.ExpiresAtUtc < now)
            .ToListAsync(cancellationToken);

        foreach (var export in expiredExports)
        {
            export.Status = ExportJobStatus.Expired;
            export.UpdatedAtUtc = now;
        }

        run.RecordsProcessed = expiredExports.Count;
        run.Summary = $"Marked {expiredExports.Count} export link(s) expired.";
        run.Status = RetentionJobStatus.Completed;
        run.CompletedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return run.RecordsProcessed;
    }

    public async Task<IReadOnlyList<AdminPrivacyQueueItemResponse>> ListAdminQueueAsync(
        CancellationToken cancellationToken = default)
    {
        return await (
            from queue in dbContext.AdminPrivacyQueueItems.AsNoTracking()
            join request in dbContext.PrivacyRequests.AsNoTracking()
                on queue.PrivacyRequestId equals request.Id
            orderby queue.CreatedAtUtc descending
            select new AdminPrivacyQueueItemResponse(
                queue.PublicId,
                request.PublicId,
                request.RequestType,
                queue.QueueStatus,
                queue.AssignedOperator,
                queue.Notes,
                queue.CreatedAtUtc,
                queue.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<(AdminPrivacyQueueItemResponse? Item, string? Error)> UpdateAdminQueueItemAsync(
        Guid queuePublicId,
        UpdateAdminQueueItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var queueItem = await dbContext.AdminPrivacyQueueItems
            .FirstOrDefaultAsync(q => q.PublicId == queuePublicId, cancellationToken);
        if (queueItem is null)
        {
            return (null, "Queue item not found.");
        }

        if (request.QueueStatus is not null)
        {
            queueItem.QueueStatus = request.QueueStatus.Value;
        }

        if (request.AssignedOperator is not null)
        {
            queueItem.AssignedOperator = string.IsNullOrWhiteSpace(request.AssignedOperator)
                ? null
                : request.AssignedOperator.Trim();
        }

        if (request.Notes is not null)
        {
            queueItem.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        }

        queueItem.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var privacyRequest = await dbContext.PrivacyRequests.AsNoTracking()
            .FirstAsync(r => r.Id == queueItem.PrivacyRequestId, cancellationToken);

        return (new AdminPrivacyQueueItemResponse(
            queueItem.PublicId,
            privacyRequest.PublicId,
            privacyRequest.RequestType,
            queueItem.QueueStatus,
            queueItem.AssignedOperator,
            queueItem.Notes,
            queueItem.CreatedAtUtc,
            queueItem.UpdatedAtUtc), null);
    }

    private async Task<bool> EnsurePrivacyAccessAsync(
        Guid tenantId,
        Capability capability,
        CancellationToken cancellationToken)
    {
        var check = await entitlementService.CheckCapabilityAsync(tenantId, capability, cancellationToken);
        return check.IsAllowed;
    }

    private async Task<ConsentRecord> UpsertConsentAsync(
        Guid tenantId,
        Guid userId,
        Guid noticeVersionId,
        ConsentType consentType,
        string purpose,
        bool granted,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.ConsentRecords
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId &&
                     c.UserId == userId &&
                     c.NoticeVersionId == noticeVersionId &&
                     c.ConsentType == consentType,
                cancellationToken);

        if (existing is not null)
        {
            existing.Granted = granted;
            existing.GrantedAtUtc = now;
            existing.WithdrawnAtUtc = granted ? null : now;
            existing.WithdrawalReason = granted ? null : existing.WithdrawalReason;
            return existing;
        }

        var consent = new ConsentRecord
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            NoticeVersionId = noticeVersionId,
            ConsentType = consentType,
            Purpose = purpose,
            Granted = granted,
            GrantedAtUtc = now
        };

        dbContext.ConsentRecords.Add(consent);
        return consent;
    }

    private static PrivacyNoticeResponse MapNotice(PrivacyNoticeVersion notice) =>
        new(notice.PublicId, notice.Version, notice.Title, notice.Summary, notice.EffectiveFromUtc, notice.IsCurrent);

    private static ConsentRecordResponse MapConsent(ConsentRecord consent, Guid noticePublicId) =>
        new(
            consent.PublicId,
            consent.ConsentType,
            consent.Purpose,
            consent.Granted,
            consent.GrantedAtUtc,
            consent.WithdrawnAtUtc,
            noticePublicId);

    private static PrivacyRequestResponse MapRequest(PrivacyRequest request)
    {
        var statusMessage = request.RequestType switch
        {
            PrivacyRequestType.Erasure =>
                "Account deletion is orchestrated over multiple steps. Backup copies may remain until the scheduled purge date and are not erased instantly.",
            PrivacyRequestType.Export =>
                "Export preparation may take time. You will receive a time-limited download link when ready.",
            PrivacyRequestType.Grievance =>
                "Grievance requests are reviewed by the platform operator queue.",
            _ => null
        };

        return new PrivacyRequestResponse(
            request.PublicId,
            request.RequestType,
            request.Status,
            request.Subject,
            request.Details,
            request.CorrectionDetails,
            request.GrievanceCategory,
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            request.CompletedAtUtc,
            statusMessage);
    }

    private static DataExportJobResponse MapExportJob(DataExportJob job) =>
        new(job.PublicId, job.Status, job.DownloadToken, job.ExpiresAtUtc, job.CreatedAtUtc);

    private static DeletionJobResponse MapDeletionJob(DeletionOrchestrationJob job) =>
        new(
            job.PublicId,
            job.Status,
            job.CurrentStep,
            job.ScheduledBackupPurgeAtUtc,
            "Deletion is staged. Backup retention purge is scheduled and does not happen instantly.");
}
