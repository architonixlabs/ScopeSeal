namespace ScopeSeal.AgreementSnapshots.Domain;

public sealed class AgreementSnapshot
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public SnapshotStatus Status { get; set; }

    public int VersionNumber { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string? CanonicalHashSha256 { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public ICollection<ScopeItem> ScopeItems { get; set; } = [];

    public ICollection<Exclusion> Exclusions { get; set; } = [];

    public ICollection<Deliverable> Deliverables { get; set; } = [];

    public ICollection<Commitment> Commitments { get; set; } = [];

    public ICollection<PaymentMilestone> PaymentMilestones { get; set; } = [];

    public ICollection<TimelineMilestone> TimelineMilestones { get; set; } = [];

    public ICollection<SnapshotDependency> Dependencies { get; set; } = [];

    public ICollection<Assumption> Assumptions { get; set; } = [];

    public ICollection<OpenQuestion> OpenQuestions { get; set; } = [];
}
