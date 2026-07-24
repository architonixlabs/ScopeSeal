namespace ScopeSeal.Approvals.Domain;

public sealed class ReviewInvitation
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid AgreementSnapshotId { get; set; }

    public Guid Token { get; set; }

    public string ReviewerEmail { get; set; } = string.Empty;

    public string? ReviewerName { get; set; }

    public InvitationStatus Status { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public Guid? RevokedByUserId { get; set; }

    public DateTime? LastAccessedAtUtc { get; set; }
}
