using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Audit.Services;
using ScopeSeal.Documents.Domain;
using ScopeSeal.Documents.Services;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Infrastructure.Services;

public sealed class UploadSessionService(
    ApplicationDbContext dbContext,
    IEntitlementService entitlementService,
    IAuditService auditService,
    IBlobStorageService blobStorage,
    IContentTypeValidator contentTypeValidator,
    IMalwareScanner malwareScanner,
    IOptions<ScopeSealOptions> options) : IUploadSessionService
{
    private readonly DocumentUploadOptions _uploadOptions = options.Value.DocumentUpload;

    public async Task<(UploadSessionSummary? Session, string? Error)> CreateSessionAsync(
        Guid tenantId,
        Guid userId,
        CreateUploadSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var capabilityCheck = await entitlementService.CheckCapabilityAsync(
            tenantId,
            Capability.CanUploadDocument,
            cancellationToken);

        if (!capabilityCheck.IsAllowed)
        {
            return (null, capabilityCheck.DenialReason ?? "Document upload is not allowed.");
        }

        if (request.ExpectedBytes <= 0)
        {
            return (null, "Expected file size must be greater than zero.");
        }

        if (request.ExpectedBytes > _uploadOptions.MaxFileBytes)
        {
            return (null, $"File exceeds the maximum upload size of {_uploadOptions.MaxFileBytes} bytes.");
        }

        var usageCheck = await entitlementService.CheckUsageAsync(
            tenantId,
            UsageMetric.StorageBytes,
            request.ExpectedBytes,
            cancellationToken);

        if (!usageCheck.IsAllowed)
        {
            return (null, usageCheck.DenialReason ?? "Storage limit reached.");
        }

        var typeValidation = contentTypeValidator.ValidateDeclaredType(
            request.DeclaredContentType,
            request.OriginalFileName);

        if (!typeValidation.IsValid)
        {
            return (null, typeValidation.Error);
        }

        var workspace = await dbContext.Workspaces
            .AsNoTracking()
            .SingleOrDefaultAsync(
                w => w.TenantId == tenantId && w.PublicId == request.WorkspacePublicId,
                cancellationToken);

        if (workspace is null)
        {
            return (null, "Workspace not found.");
        }

        var now = DateTime.UtcNow;
        var serverFileName = $"{Guid.NewGuid():N}{GetSafeExtension(request.OriginalFileName)}";
        var quarantinePath = $"{tenantId:N}/{workspace.Id:N}/{serverFileName}";

        var session = new UploadSession
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = workspace.Id,
            OriginalFileName = Path.GetFileName(request.OriginalFileName.Trim()),
            DeclaredContentType = request.DeclaredContentType.Trim().ToLowerInvariant(),
            ServerFileName = serverFileName,
            QuarantineBlobPath = quarantinePath,
            ExpectedBytes = request.ExpectedBytes,
            Status = UploadSessionStatus.Pending,
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddHours(_uploadOptions.SessionExpirationHours)
        };

        dbContext.UploadSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.UploadSessionCreated,
            "UploadSession",
            session.PublicId,
            userId,
            $"Upload session created for '{session.OriginalFileName}'.",
            cancellationToken);

        return (await MapSessionAsync(session, workspace.PublicId, cancellationToken), null);
    }

    public async Task<(UploadSessionSummary? Session, string? Error)> UploadContentAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid sessionPublicId,
        Stream content,
        long contentLength,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionEntityAsync(tenantId, workspacePublicId, sessionPublicId, cancellationToken);
        if (session is null)
        {
            return (null, "Upload session not found.");
        }

        if (session.ExpiresAtUtc <= DateTime.UtcNow)
        {
            session.Status = UploadSessionStatus.Expired;
            await dbContext.SaveChangesAsync(cancellationToken);
            return (null, "Upload session has expired.");
        }

        if (session.Status is not (UploadSessionStatus.Pending or UploadSessionStatus.Uploading))
        {
            return (null, "Upload session is not accepting content.");
        }

        if (contentLength <= 0)
        {
            return (null, "Uploaded content is empty.");
        }

        if (contentLength > _uploadOptions.MaxFileBytes)
        {
            return (null, $"File exceeds the maximum upload size of {_uploadOptions.MaxFileBytes} bytes.");
        }

        if (session.ExpectedBytes.HasValue && contentLength > session.ExpectedBytes.Value)
        {
            return (null, "Uploaded content exceeds the declared file size.");
        }

        var usageCheck = await entitlementService.CheckUsageAsync(
            tenantId,
            UsageMetric.StorageBytes,
            contentLength,
            cancellationToken);

        if (!usageCheck.IsAllowed)
        {
            return (null, usageCheck.DenialReason ?? "Storage limit reached.");
        }

        session.Status = UploadSessionStatus.Uploading;
        await dbContext.SaveChangesAsync(cancellationToken);

        await blobStorage.WriteAsync(
            BlobContainerKind.Quarantine,
            session.QuarantineBlobPath,
            content,
            session.DeclaredContentType,
            cancellationToken);

        var header = new byte[512];
        await using (var blobStream = await blobStorage.OpenReadAsync(
            BlobContainerKind.Quarantine,
            session.QuarantineBlobPath,
            cancellationToken))
        {
            if (blobStream is null)
            {
                return (null, "Uploaded content could not be verified.");
            }

            var read = await blobStream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            var contentValidation = contentTypeValidator.ValidateContent(header.AsSpan(0, read), session.DeclaredContentType);
            if (!contentValidation.IsValid)
            {
                session.Status = UploadSessionStatus.Rejected;
                session.RejectionReason = contentValidation.Error;
                await dbContext.SaveChangesAsync(cancellationToken);
                await blobStorage.DeleteAsync(BlobContainerKind.Quarantine, session.QuarantineBlobPath, cancellationToken);
                return (null, contentValidation.Error);
            }
        }

        session.UploadedBytes = contentLength;
        session.Status = UploadSessionStatus.Quarantined;
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await MapSessionAsync(session, workspacePublicId, cancellationToken), null);
    }

    public async Task<(CompleteUploadResult? Result, string? Error)> CompleteSessionAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid sessionPublicId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionEntityAsync(tenantId, workspacePublicId, sessionPublicId, cancellationToken);
        if (session is null)
        {
            return (null, "Upload session not found.");
        }

        if (session.Status != UploadSessionStatus.Quarantined)
        {
            return (null, "Upload session is not ready to complete.");
        }

        session.Status = UploadSessionStatus.Scanning;
        await dbContext.SaveChangesAsync(cancellationToken);

        await using var quarantineStream = await blobStorage.OpenReadAsync(
            BlobContainerKind.Quarantine,
            session.QuarantineBlobPath,
            cancellationToken);

        if (quarantineStream is null)
        {
            session.Status = UploadSessionStatus.Rejected;
            session.RejectionReason = "Quarantined content is missing.";
            await dbContext.SaveChangesAsync(cancellationToken);
            return (null, session.RejectionReason);
        }

        // Azure blob streams are not seekable; buffer once for hash + malware scan.
        await using var bufferedContent = new MemoryStream();
        await quarantineStream.CopyToAsync(bufferedContent, cancellationToken);
        bufferedContent.Position = 0;

        var hashBytes = await SHA256.HashDataAsync(bufferedContent, cancellationToken);
        var hashValue = Convert.ToHexString(hashBytes).ToLowerInvariant();
        bufferedContent.Position = 0;

        var scanOutcome = await malwareScanner.ScanAsync(
            bufferedContent,
            session.DeclaredContentType,
            cancellationToken);

        if (scanOutcome.Status == MalwareScanStatus.Infected)
        {
            session.Status = UploadSessionStatus.Rejected;
            session.RejectionReason = scanOutcome.Details ?? "Malware detected.";
            await dbContext.SaveChangesAsync(cancellationToken);
            await blobStorage.DeleteAsync(BlobContainerKind.Quarantine, session.QuarantineBlobPath, cancellationToken);

            await auditService.RecordAsync(
                tenantId,
                AuditEventType.UploadRejected,
                "UploadSession",
                session.PublicId,
                userId,
                session.RejectionReason,
                cancellationToken);

            return (null, session.RejectionReason);
        }

        if (scanOutcome.Status == MalwareScanStatus.Error)
        {
            session.Status = UploadSessionStatus.Rejected;
            session.RejectionReason = scanOutcome.Details ?? "Malware scan failed.";
            await dbContext.SaveChangesAsync(cancellationToken);
            return (null, session.RejectionReason);
        }

        var permanentPath = $"{tenantId:N}/{session.WorkspaceId:N}/{session.ServerFileName}";
        await blobStorage.CopyAsync(
            BlobContainerKind.Quarantine,
            session.QuarantineBlobPath,
            BlobContainerKind.Permanent,
            permanentPath,
            cancellationToken);

        var now = DateTime.UtcNow;
        var document = new Document
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = session.WorkspaceId,
            OriginalFileName = session.OriginalFileName,
            ContentType = session.DeclaredContentType,
            Status = DocumentStatus.Available,
            SizeBytes = session.UploadedBytes ?? 0,
            CreatedByUserId = userId,
            CreatedAtUtc = now
        };

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            DocumentId = document.Id,
            VersionNumber = 1,
            CreatedAtUtc = now,
            Blob = new DocumentBlob
            {
                Id = Guid.NewGuid(),
                Container = _uploadOptions.PermanentContainer,
                StoragePath = permanentPath,
                SizeBytes = session.UploadedBytes ?? 0
            },
            Hash = new DocumentHash
            {
                Id = Guid.NewGuid(),
                Algorithm = "SHA256",
                HashValue = hashValue
            },
            MalwareScan = new MalwareScanResult
            {
                Id = Guid.NewGuid(),
                Status = scanOutcome.Status,
                ScannerName = scanOutcome.ScannerName,
                ScannedAtUtc = now,
                Details = scanOutcome.Details
            }
        };

        var processingJob = new ProcessingJob
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            JobType = ProcessingJobType.PreviewGeneration,
            Status = ProcessingJobStatus.Pending,
            CreatedAtUtc = now
        };

        version.ProcessingJobs.Add(processingJob);
        document.Versions.Add(version);

        dbContext.Documents.Add(document);
        session.Status = UploadSessionStatus.Completed;
        session.DocumentId = document.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        await entitlementService.RecordUsageAsync(
            tenantId,
            UsageMetric.StorageBytes,
            document.SizeBytes,
            cancellationToken);

        await blobStorage.DeleteAsync(BlobContainerKind.Quarantine, session.QuarantineBlobPath, cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.DocumentUploaded,
            "Document",
            document.PublicId,
            userId,
            $"Document '{document.OriginalFileName}' uploaded.",
            cancellationToken);

        var sessionSummary = await MapSessionAsync(session, workspacePublicId, cancellationToken);
        var documentSummary = DocumentService.MapSummary(document, workspacePublicId);
        return (new CompleteUploadResult(sessionSummary!, documentSummary), null);
    }

    public async Task<UploadSessionSummary?> GetSessionAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid sessionPublicId,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionEntityAsync(tenantId, workspacePublicId, sessionPublicId, cancellationToken);
        return session is null ? null : await MapSessionAsync(session, workspacePublicId, cancellationToken);
    }

    private async Task<UploadSession?> GetSessionEntityAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid sessionPublicId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UploadSessions
            .SingleOrDefaultAsync(
                s => s.TenantId == tenantId &&
                     s.PublicId == sessionPublicId &&
                     dbContext.Workspaces.Any(w =>
                         w.Id == s.WorkspaceId &&
                         w.TenantId == tenantId &&
                         w.PublicId == workspacePublicId),
                cancellationToken);
    }

    private async Task<UploadSessionSummary> MapSessionAsync(
        UploadSession session,
        Guid workspacePublicId,
        CancellationToken cancellationToken)
    {
        Guid? documentPublicId = null;
        if (session.DocumentId.HasValue)
        {
            documentPublicId = await dbContext.Documents
                .AsNoTracking()
                .Where(d => d.Id == session.DocumentId.Value && d.TenantId == session.TenantId)
                .Select(d => d.PublicId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return new UploadSessionSummary(
            session.PublicId,
            workspacePublicId,
            session.OriginalFileName,
            session.DeclaredContentType,
            session.ServerFileName,
            session.ExpectedBytes,
            session.UploadedBytes,
            session.Status.ToString(),
            session.RejectionReason,
            session.CreatedAtUtc,
            session.ExpiresAtUtc,
            documentPublicId == Guid.Empty ? null : documentPublicId);
    }

    private static string GetSafeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension.Length is > 0 and <= 10 ? extension : string.Empty;
    }
}
