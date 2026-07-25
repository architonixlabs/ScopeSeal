namespace ScopeSeal.Privacy.Domain;

public sealed class AdminPrivacyQueueItem
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid PrivacyRequestId { get; set; }

    public AdminQueueStatus QueueStatus { get; set; }

    public string? AssignedOperator { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
