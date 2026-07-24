namespace ScopeSeal.Documents.Domain;

public sealed class UploadSession
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid WorkspaceId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string DeclaredContentType { get; set; } = string.Empty;

    public string ServerFileName { get; set; } = string.Empty;

    public string QuarantineBlobPath { get; set; } = string.Empty;

    public long? ExpectedBytes { get; set; }

    public long? UploadedBytes { get; set; }

    public UploadSessionStatus Status { get; set; } = UploadSessionStatus.Pending;

    public string? RejectionReason { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public Guid? DocumentId { get; set; }
}
