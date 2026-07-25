namespace ScopeSeal.Documents.Domain;

public sealed class DocumentBlob
{
    public Guid Id { get; set; }

    public Guid DocumentVersionId { get; set; }

    public DocumentVersion DocumentVersion { get; set; } = null!;

    public string Container { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
}
