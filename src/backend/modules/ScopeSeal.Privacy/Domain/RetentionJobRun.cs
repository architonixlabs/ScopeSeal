namespace ScopeSeal.Privacy.Domain;

public sealed class RetentionJobRun
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public string JobType { get; set; } = string.Empty;

    public RetentionJobStatus Status { get; set; }

    public int RecordsProcessed { get; set; }

    public string Summary { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}
