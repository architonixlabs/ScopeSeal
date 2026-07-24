using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScopeSeal.Documents.Domain;
using ScopeSeal.Documents.Services;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Infrastructure.Services;

public sealed class DocumentService(
    ApplicationDbContext dbContext,
    IBlobStorageService blobStorage,
    IOptions<ScopeSealOptions> options) : IDocumentService
{
    private readonly DocumentUploadOptions _uploadOptions = options.Value.DocumentUpload;

    public async Task<IReadOnlyList<DocumentSummary>?> ListDocumentsAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return null;
        }

        var documents = await dbContext.Documents
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.WorkspaceId == workspaceId.Value)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Include(d => d.Versions)
                .ThenInclude(v => v.Hash)
            .Include(d => d.Versions)
                .ThenInclude(v => v.MalwareScan)
            .ToListAsync(cancellationToken);

        return documents.Select(d => MapSummary(d, workspacePublicId)).ToList();
    }

    public async Task<DocumentSummary?> GetDocumentAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid documentPublicId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentEntityAsync(
            tenantId,
            workspacePublicId,
            documentPublicId,
            cancellationToken);

        if (document is null)
        {
            return null;
        }

        var workspace = await dbContext.Workspaces
            .AsNoTracking()
            .SingleAsync(w => w.Id == document.WorkspaceId, cancellationToken);

        return MapSummary(document, workspace.PublicId);
    }

    public async Task<(SignedDownloadToken? Token, string? Error)> CreateDownloadTokenAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid documentPublicId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentEntityAsync(
            tenantId,
            workspacePublicId,
            documentPublicId,
            cancellationToken);

        if (document is null)
        {
            return (null, "Document not found.");
        }

        if (document.Status != DocumentStatus.Available)
        {
            return (null, "Document is not available for download.");
        }

        var now = DateTime.UtcNow;
        var token = new DocumentDownloadToken
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentId = document.Id,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(_uploadOptions.DownloadTokenExpirationMinutes)
        };

        dbContext.DocumentDownloadTokens.Add(token);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (new SignedDownloadToken(
            token.Token,
            token.ExpiresAtUtc,
            $"/api/v1/tenants/{{tenantPublicId}}/documents/download?token={token.Token}"), null);
    }

    public async Task<(DocumentDownloadInfo? Download, string? Error)> DownloadWithTokenAsync(
        Guid tenantId,
        Guid token,
        CancellationToken cancellationToken = default)
    {
        var downloadToken = await dbContext.DocumentDownloadTokens
            .SingleOrDefaultAsync(
                t => t.TenantId == tenantId && t.Token == token,
                cancellationToken);

        if (downloadToken is null || downloadToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return (null, "Download token is invalid or expired.");
        }

        var document = await dbContext.Documents
            .Include(d => d.Versions)
                .ThenInclude(v => v.Blob)
            .SingleOrDefaultAsync(
                d => d.Id == downloadToken.DocumentId && d.TenantId == tenantId,
                cancellationToken);

        if (document is null || document.Status != DocumentStatus.Available)
        {
            return (null, "Document not found.");
        }

        var latestVersion = document.Versions
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        if (latestVersion?.Blob is null)
        {
            return (null, "Document blob not found.");
        }

        var stream = await blobStorage.OpenReadAsync(
            BlobContainerKind.Permanent,
            latestVersion.Blob.StoragePath,
            cancellationToken);

        if (stream is null)
        {
            return (null, "Document content is unavailable.");
        }

        return (new DocumentDownloadInfo(
            document.OriginalFileName,
            document.ContentType,
            document.SizeBytes,
            stream), null);
    }

    internal static DocumentSummary MapSummary(Document document, Guid workspacePublicId)
    {
        var latestVersion = document.Versions
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        DocumentPreviewMetadata? preview = null;
        if (latestVersion?.Hash is not null && latestVersion.MalwareScan is not null)
        {
            preview = new DocumentPreviewMetadata(
                document.ContentType,
                document.SizeBytes,
                latestVersion.Hash.Algorithm,
                latestVersion.Hash.HashValue,
                latestVersion.MalwareScan.Status.ToString(),
                latestVersion.MalwareScan.Status == MalwareScanStatus.Clean &&
                IsPreviewSafeContentType(document.ContentType));
        }

        return new DocumentSummary(
            document.PublicId,
            workspacePublicId,
            document.OriginalFileName,
            document.ContentType,
            document.Status.ToString(),
            document.SizeBytes,
            document.CreatedAtUtc,
            preview);
    }

    private async Task<Document?> GetDocumentEntityAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid documentPublicId,
        CancellationToken cancellationToken)
    {
        var workspaceId = await ResolveWorkspaceIdAsync(tenantId, workspacePublicId, cancellationToken);
        if (workspaceId is null)
        {
            return null;
        }

        return await dbContext.Documents
            .Include(d => d.Versions)
                .ThenInclude(v => v.Hash)
            .Include(d => d.Versions)
                .ThenInclude(v => v.MalwareScan)
            .SingleOrDefaultAsync(
                d => d.TenantId == tenantId &&
                     d.WorkspaceId == workspaceId.Value &&
                     d.PublicId == documentPublicId,
                cancellationToken);
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

    private static bool IsPreviewSafeContentType(string contentType) =>
        contentType is "application/pdf" or "image/png" or "image/jpeg" or "image/webp" or "text/plain" or "text/csv";
}
