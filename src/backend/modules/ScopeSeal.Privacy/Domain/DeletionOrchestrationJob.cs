namespace ScopeSeal.Privacy.Domain;

public sealed class DeletionOrchestrationJob
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid UserId { get; set; }

    public Guid PrivacyRequestId { get; set; }

    public DeletionJobStatus Status { get; set; }

    public DeletionStep CurrentStep { get; set; }

    public DateTime ScheduledBackupPurgeAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
