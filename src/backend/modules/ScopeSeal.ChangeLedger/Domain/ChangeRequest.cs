namespace ScopeSeal.ChangeLedger.Domain;

public sealed class ChangeRequest
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid SourceSnapshotId { get; set; }

    public Guid? ResultSnapshotId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public ChangeRequestStatus Status { get; set; }

    public Guid ProposedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? ImplementedAtUtc { get; set; }

    public ICollection<ChangeImpact> Impacts { get; set; } = [];

    public ICollection<ChangeDecision> Decisions { get; set; } = [];
}
