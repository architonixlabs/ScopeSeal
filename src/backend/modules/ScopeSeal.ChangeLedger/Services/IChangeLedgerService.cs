using ScopeSeal.AgreementSnapshots.Services;
using ScopeSeal.ChangeLedger.Domain;

namespace ScopeSeal.ChangeLedger.Services;

public sealed record ChangeImpactInput(
    ChangeImpactType ImpactType,
    string Description,
    long? AmountMinorUnits,
    string? CurrencyCode,
    int? ScheduleDaysDelta);

public sealed record ChangeImpactDetail(
    Guid PublicId,
    ChangeImpactType ImpactType,
    string Description,
    long? AmountMinorUnits,
    string? CurrencyCode,
    int? ScheduleDaysDelta);

public sealed record ChangeDecisionDetail(
    Guid PublicId,
    ChangeRequestStatus PreviousStatus,
    ChangeRequestStatus NewStatus,
    string? DecisionNote,
    DateTime DecidedAtUtc);

public sealed record ChangeRequestSummary(
    Guid PublicId,
    string Title,
    ChangeRequestStatus Status,
    Guid SourceSnapshotPublicId,
    Guid? ResultSnapshotPublicId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ChangeRequestDetail(
    Guid PublicId,
    string Title,
    string Reason,
    ChangeRequestStatus Status,
    Guid SourceSnapshotPublicId,
    int SourceSnapshotVersionNumber,
    Guid? ResultSnapshotPublicId,
    int? ResultSnapshotVersionNumber,
    IReadOnlyList<ChangeImpactDetail> Impacts,
    IReadOnlyList<ChangeDecisionDetail> Decisions,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ImplementedAtUtc);

public sealed record CreateChangeRequestRequest(
    Guid SourceSnapshotPublicId,
    string Title,
    string Reason,
    IReadOnlyList<ChangeImpactInput>? Impacts);

public sealed record TransitionChangeRequestRequest(
    ChangeRequestStatus NewStatus,
    string? DecisionNote);

public sealed record AcceptChangeRequestResult(
    ChangeRequestDetail ChangeRequest,
    AgreementSnapshotDetail DraftSnapshot);

public sealed record SectionDiffEntry(
    string SectionName,
    string ChangeType,
    Guid? PublicId,
    string? Title,
    string? PreviousTitle,
    string? Description,
    string? PreviousDescription);

public sealed record SnapshotDiffDetail(
    Guid FromSnapshotPublicId,
    int FromVersionNumber,
    Guid ToSnapshotPublicId,
    int ToVersionNumber,
    IReadOnlyList<SectionDiffEntry> Changes);

public interface IChangeLedgerService
{
    Task<(ChangeRequestDetail? ChangeRequest, string? Error)> CreateChangeRequestAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid userId,
        CreateChangeRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChangeRequestSummary>?> ListChangeRequestsAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken = default);

    Task<ChangeRequestDetail?> GetChangeRequestAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid changeRequestPublicId,
        CancellationToken cancellationToken = default);

    Task<(ChangeRequestDetail? ChangeRequest, string? Error)> TransitionChangeRequestAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid changeRequestPublicId,
        Guid userId,
        TransitionChangeRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<(AcceptChangeRequestResult? Result, string? Error)> AcceptChangeRequestAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid changeRequestPublicId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SnapshotDiffDetail?> GetSnapshotDiffAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid fromSnapshotPublicId,
        Guid toSnapshotPublicId,
        CancellationToken cancellationToken = default);
}
