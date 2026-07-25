using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

public sealed class ProcessingJobProcessor(
    ApplicationDbContext dbContext,
    IBlobStorageService blobStorage,
    AiExtractionProviderFactory providerFactory,
    IExtractionSchemaValidator schemaValidator,
    IEntitlementService entitlementService,
    IAuditService auditService,
    IOptions<ScopeSealOptions> scopeSealOptions,
    ILogger<ProcessingJobProcessor> logger) : IProcessingJobProcessor
{
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        if (scopeSealOptions.Value.Ai.KillSwitchEnabled)
        {
            return 0;
        }

        var batchSize = scopeSealOptions.Value.Ai.MaxExtractionJobsPerBatch;
        var pendingJobs = await dbContext.ProcessingJobs
            .Where(j => j.Status == ProcessingJobStatus.Pending &&
                        j.JobType == ProcessingJobType.TextExtraction)
            .OrderBy(j => j.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var job in pendingJobs)
        {
            if (await ProcessJobAsync(job, cancellationToken))
            {
                processed++;
            }
        }

        return processed;
    }

    private async Task<bool> ProcessJobAsync(ProcessingJob job, CancellationToken cancellationToken)
    {
        var claimed = await dbContext.ProcessingJobs
            .Where(j => j.Id == job.Id && j.Status == ProcessingJobStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(j => j.Status, ProcessingJobStatus.Running),
                cancellationToken);

        if (claimed == 0)
        {
            return false;
        }

        var run = await dbContext.ExtractionRuns
            .SingleOrDefaultAsync(r => r.ProcessingJobId == job.Id, cancellationToken);

        if (run is null)
        {
            await MarkJobFailedAsync(job.Id, "Extraction run not found.", cancellationToken);
            return false;
        }

        run.Status = ExtractionRunStatus.Processing;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var version = await dbContext.DocumentVersions
                .Include(v => v.Document)
                .Include(v => v.Hash)
                .Include(v => v.Blob)
                .SingleAsync(v => v.Id == job.DocumentVersionId, cancellationToken);

            if (version.Blob is null || version.Hash is null)
            {
                throw new InvalidOperationException("Document blob or hash is unavailable.");
            }

            await using var contentStream = await blobStorage.OpenReadAsync(
                BlobContainerKind.Permanent,
                version.Blob.StoragePath,
                cancellationToken);

            if (contentStream is null)
            {
                throw new InvalidOperationException("Document content is unavailable.");
            }

            var provider = providerFactory.Resolve();
            var providerContext = new ExtractionProviderContext(
                job.TenantId,
                version.Document.OriginalFileName,
                version.Document.ContentType,
                version.Hash.HashValue,
                contentStream);

            var providerResult = await provider.ExtractAsync(providerContext, cancellationToken);
            var validation = schemaValidator.ValidateFacts(providerResult.Facts);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.Error ?? "Extraction output failed validation.");
            }

            var now = DateTime.UtcNow;
            foreach (var fact in providerResult.Facts)
            {
                dbContext.ExtractedFacts.Add(new ExtractedFact
                {
                    Id = Guid.NewGuid(),
                    PublicId = Guid.NewGuid(),
                    TenantId = job.TenantId,
                    ExtractionRunId = run.Id,
                    SectionType = fact.SectionType,
                    Title = fact.Title.Trim(),
                    Description = fact.Description?.Trim(),
                    AmountMinorUnits = fact.AmountMinorUnits,
                    CurrencyCode = fact.CurrencyCode?.Trim().ToUpperInvariant(),
                    ConfidenceScore = fact.ConfidenceScore,
                    ReviewStatus = FactReviewStatus.Draft,
                    SourceDocumentName = version.Document.OriginalFileName,
                    SourceHashValue = version.Hash.HashValue,
                    SourcePageNumber = fact.Source.PageNumber,
                    SourceExcerpt = fact.Source.Excerpt?.Trim(),
                    CreatedAtUtc = now
                });
            }

            run.Status = ExtractionRunStatus.Completed;
            run.CompletedAtUtc = now;
            run.ErrorMessage = null;

            job.Status = ProcessingJobStatus.Completed;
            job.CompletedAtUtc = now;
            job.ErrorMessage = null;

            await dbContext.SaveChangesAsync(cancellationToken);

            await entitlementService.RecordUsageAsync(
                job.TenantId,
                UsageMetric.AiExtractionJobsThisMonth,
                increment: 1,
                cancellationToken);

            await auditService.RecordAsync(
                job.TenantId,
                AuditEventType.ExtractionRunCompleted,
                "ExtractionRun",
                run.PublicId,
                run.CreatedByUserId,
                $"AI extraction completed with {providerResult.Facts.Count} draft facts using {providerResult.ProviderName}.",
                cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Processing job {JobId} failed.", job.PublicId);
            await MarkRunFailedAsync(run.Id, job.Id, ex.Message, cancellationToken);
            return false;
        }
    }

    private async Task MarkRunFailedAsync(
        Guid runId,
        Guid jobId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await dbContext.ExtractionRuns
            .Where(r => r.Id == runId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, ExtractionRunStatus.Failed)
                    .SetProperty(r => r.ErrorMessage, errorMessage)
                    .SetProperty(r => r.CompletedAtUtc, now),
                cancellationToken);

        await MarkJobFailedAsync(jobId, errorMessage, cancellationToken);

        var run = await dbContext.ExtractionRuns
            .AsNoTracking()
            .SingleAsync(r => r.Id == runId, cancellationToken);

        await auditService.RecordAsync(
            run.TenantId,
            AuditEventType.ExtractionRunFailed,
            "ExtractionRun",
            run.PublicId,
            run.CreatedByUserId,
            $"AI extraction failed: {errorMessage}",
            cancellationToken);
    }

    private async Task MarkJobFailedAsync(
        Guid jobId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await dbContext.ProcessingJobs
            .Where(j => j.Id == jobId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(j => j.Status, ProcessingJobStatus.Failed)
                    .SetProperty(j => j.ErrorMessage, errorMessage)
                    .SetProperty(j => j.CompletedAtUtc, now),
                cancellationToken);
    }
}
