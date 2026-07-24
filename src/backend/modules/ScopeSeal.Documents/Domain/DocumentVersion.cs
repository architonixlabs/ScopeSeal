namespace ScopeSeal.Documents.Domain;

public sealed class DocumentVersion
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid DocumentId { get; set; }

    public Document Document { get; set; } = null!;

    public int VersionNumber { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DocumentBlob? Blob { get; set; }

    public DocumentHash? Hash { get; set; }

    public MalwareScanResult? MalwareScan { get; set; }

    public ICollection<ProcessingJob> ProcessingJobs { get; set; } = [];
}
