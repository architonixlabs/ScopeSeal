namespace ScopeSeal.Documents.Services;

public sealed record CreateUploadSessionRequest(
    Guid WorkspacePublicId,
    string OriginalFileName,
    string DeclaredContentType,
    long ExpectedBytes);

public sealed record UploadSessionSummary(
    Guid PublicId,
    Guid WorkspacePublicId,
    string OriginalFileName,
    string DeclaredContentType,
    string ServerFileName,
    long? ExpectedBytes,
    long? UploadedBytes,
    string Status,
    string? RejectionReason,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    Guid? DocumentPublicId);

public sealed record CompleteUploadResult(
    UploadSessionSummary Session,
    DocumentSummary? Document);

public interface IUploadSessionService
{
    Task<(UploadSessionSummary? Session, string? Error)> CreateSessionAsync(
        Guid tenantId,
        Guid userId,
        CreateUploadSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<(UploadSessionSummary? Session, string? Error)> UploadContentAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid sessionPublicId,
        Stream content,
        long contentLength,
        CancellationToken cancellationToken = default);

    Task<(CompleteUploadResult? Result, string? Error)> CompleteSessionAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid sessionPublicId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UploadSessionSummary?> GetSessionAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid sessionPublicId,
        CancellationToken cancellationToken = default);
}
