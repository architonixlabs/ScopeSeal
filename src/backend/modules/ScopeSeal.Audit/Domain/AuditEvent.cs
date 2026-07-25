namespace ScopeSeal.Audit.Domain;

public sealed class AuditEvent
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public AuditEventType EventType { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityPublicId { get; set; }

    public Guid? ActorUserId { get; set; }

    public string? Summary { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
