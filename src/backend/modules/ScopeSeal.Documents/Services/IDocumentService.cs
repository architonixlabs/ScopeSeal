namespace ScopeSeal.Documents.Services;

public sealed record DocumentSummary(
    Guid PublicId,
    Guid WorkspacePublicId,
    string OriginalFileName,
    string ContentType,
    string Status,
    long SizeBytes,
    DateTime CreatedAtUtc,
    DocumentPreviewMetadata? Preview);

public sealed record DocumentPreviewMetadata(
    string ContentType,
    long SizeBytes,
    string HashAlgorithm,
    string HashValue,
    string MalwareScanStatus,
    bool IsPreviewSafe);

public sealed record DocumentDownloadInfo(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content);

public sealed record SignedDownloadToken(
    Guid Token,
    DateTime ExpiresAtUtc,
    string DownloadPath);

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentSummary>?> ListDocumentsAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken = default);

    Task<DocumentSummary?> GetDocumentAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid documentPublicId,
        CancellationToken cancellationToken = default);

    Task<(SignedDownloadToken? Token, string? Error)> CreateDownloadTokenAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid documentPublicId,
        CancellationToken cancellationToken = default);

    Task<(DocumentDownloadInfo? Download, string? Error)> DownloadWithTokenAsync(
        Guid tenantId,
        Guid token,
        CancellationToken cancellationToken = default);
}
