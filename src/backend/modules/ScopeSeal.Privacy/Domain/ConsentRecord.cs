namespace ScopeSeal.Privacy.Domain;

public sealed class ConsentRecord
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid UserId { get; set; }

    public Guid NoticeVersionId { get; set; }

    public ConsentType ConsentType { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public bool Granted { get; set; }

    public DateTime GrantedAtUtc { get; set; }

    public DateTime? WithdrawnAtUtc { get; set; }

    public string? WithdrawalReason { get; set; }
}
