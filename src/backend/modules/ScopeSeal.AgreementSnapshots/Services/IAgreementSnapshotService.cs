using ScopeSeal.AgreementSnapshots.Domain;

namespace ScopeSeal.AgreementSnapshots.Services;

public sealed record AgreementSnapshotSummary(
    Guid PublicId,
    string Title,
    string? Description,
    SnapshotStatus Status,
    int VersionNumber,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record SectionItemDetail(
    Guid PublicId,
    int SortOrder,
    string Title,
    string? Description);

public sealed record PaymentMilestoneDetail(
    Guid PublicId,
    int SortOrder,
    string Title,
    string? Description,
    long? AmountMinorUnits,
    string? CurrencyCode,
    DateTime? DueDateUtc);

public sealed record TimelineMilestoneDetail(
    Guid PublicId,
    int SortOrder,
    string Title,
    string? Description,
    DateTime? TargetDateUtc);

public sealed record AgreementSnapshotDetail(
    Guid PublicId,
    string Title,
    string? Description,
    SnapshotStatus Status,
    int VersionNumber,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<SectionItemDetail> ScopeItems,
    IReadOnlyList<SectionItemDetail> Exclusions,
    IReadOnlyList<SectionItemDetail> Deliverables,
    IReadOnlyList<SectionItemDetail> Commitments,
    IReadOnlyList<PaymentMilestoneDetail> PaymentMilestones,
    IReadOnlyList<TimelineMilestoneDetail> TimelineMilestones,
    IReadOnlyList<SectionItemDetail> Dependencies,
    IReadOnlyList<SectionItemDetail> Assumptions,
    IReadOnlyList<SectionItemDetail> OpenQuestions);

public sealed record CreateAgreementSnapshotRequest(
    string Title,
    string? Description);

public sealed record SectionItemInput(
    Guid? PublicId,
    int SortOrder,
    string Title,
    string? Description);

public sealed record PaymentMilestoneInput(
    Guid? PublicId,
    int SortOrder,
    string Title,
    string? Description,
    long? AmountMinorUnits,
    string? CurrencyCode,
    DateTime? DueDateUtc);

public sealed record TimelineMilestoneInput(
    Guid? PublicId,
    int SortOrder,
    string Title,
    string? Description,
    DateTime? TargetDateUtc);

public sealed record UpdateAgreementSnapshotRequest(
    string Title,
    string? Description,
    DateTime ExpectedUpdatedAtUtc,
    IReadOnlyList<SectionItemInput> ScopeItems,
    IReadOnlyList<SectionItemInput> Exclusions,
    IReadOnlyList<SectionItemInput> Deliverables,
    IReadOnlyList<SectionItemInput> Commitments,
    IReadOnlyList<PaymentMilestoneInput> PaymentMilestones,
    IReadOnlyList<TimelineMilestoneInput> TimelineMilestones,
    IReadOnlyList<SectionItemInput> Dependencies,
    IReadOnlyList<SectionItemInput> Assumptions,
    IReadOnlyList<SectionItemInput> OpenQuestions);

public interface IAgreementSnapshotService
{
    Task<IReadOnlyList<AgreementSnapshotSummary>?> ListSnapshotsAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken = default);

    Task<AgreementSnapshotDetail?> GetSnapshotAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        CancellationToken cancellationToken = default);

    Task<(AgreementSnapshotDetail? Snapshot, string? Error)> CreateSnapshotAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid userId,
        CreateAgreementSnapshotRequest request,
        CancellationToken cancellationToken = default);

    Task<(AgreementSnapshotDetail? Snapshot, string? Error, bool IsConcurrencyConflict)> UpdateSnapshotAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        Guid userId,
        UpdateAgreementSnapshotRequest request,
        CancellationToken cancellationToken = default);
}
