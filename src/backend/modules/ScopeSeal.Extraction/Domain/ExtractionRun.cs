namespace ScopeSeal.Extraction.Domain;

public sealed class ExtractionRun
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid DocumentId { get; set; }

    public Guid? AgreementSnapshotId { get; set; }

    public Guid ProcessingJobId { get; set; }

    public ExtractionRunStatus Status { get; set; } = ExtractionRunStatus.Pending;

    public string AiMode { get; set; } = "ManualOnly";

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserId { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<ExtractedFact> Facts { get; set; } = [];
}
