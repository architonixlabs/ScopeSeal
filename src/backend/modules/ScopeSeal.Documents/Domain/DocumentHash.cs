namespace ScopeSeal.Documents.Domain;

public sealed class DocumentHash
{
    public Guid Id { get; set; }

    public Guid DocumentVersionId { get; set; }

    public DocumentVersion DocumentVersion { get; set; } = null!;

    public string Algorithm { get; set; } = "SHA256";

    public string HashValue { get; set; } = string.Empty;
}
