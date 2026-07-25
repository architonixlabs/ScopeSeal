namespace ScopeSeal.Administration.Domain;

public sealed class DeadLetterJob
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public string JobCategory { get; set; } = string.Empty;

    public Guid SourceJobPublicId { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public DateTime FailedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? RequeuedAtUtc { get; set; }

    public DeadLetterStatus Status { get; set; } = DeadLetterStatus.Open;
}
