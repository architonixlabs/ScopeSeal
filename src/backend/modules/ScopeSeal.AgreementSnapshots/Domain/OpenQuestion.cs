namespace ScopeSeal.AgreementSnapshots.Domain;

public sealed class OpenQuestion
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid AgreementSnapshotId { get; set; }

    public int SortOrder { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public AgreementSnapshot? AgreementSnapshot { get; set; }
}
