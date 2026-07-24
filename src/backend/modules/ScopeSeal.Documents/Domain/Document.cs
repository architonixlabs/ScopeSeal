namespace ScopeSeal.Documents.Domain;

public sealed class Document
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid WorkspaceId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; } = DocumentStatus.Processing;

    public long SizeBytes { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<DocumentVersion> Versions { get; set; } = [];
}
