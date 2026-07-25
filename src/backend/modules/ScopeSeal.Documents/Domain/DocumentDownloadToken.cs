namespace ScopeSeal.Documents.Domain;

public sealed class DocumentDownloadToken
{
    public Guid Id { get; set; }

    public Guid Token { get; set; }

    public Guid TenantId { get; set; }

    public Guid DocumentId { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
