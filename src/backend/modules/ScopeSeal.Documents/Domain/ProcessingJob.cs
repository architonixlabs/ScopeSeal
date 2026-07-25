namespace ScopeSeal.Documents.Domain;

public sealed class ProcessingJob
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid DocumentVersionId { get; set; }

    public DocumentVersion DocumentVersion { get; set; } = null!;

    public ProcessingJobType JobType { get; set; }

    public ProcessingJobStatus Status { get; set; } = ProcessingJobStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }
}
