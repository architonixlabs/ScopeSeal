namespace ScopeSeal.Approvals.Domain;

public sealed class ChangeSuggestion
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid AgreementSnapshotId { get; set; }

    public Guid? ReviewInvitationId { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string SectionReference { get; set; } = string.Empty;

    public string SuggestedChange { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
