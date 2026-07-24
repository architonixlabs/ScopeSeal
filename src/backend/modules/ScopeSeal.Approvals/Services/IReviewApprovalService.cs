using ScopeSeal.AgreementSnapshots.Domain;
using ScopeSeal.AgreementSnapshots.Services;

namespace ScopeSeal.Approvals.Services;

public sealed record ReviewInvitationSummary(
    Guid PublicId,
    string ReviewerEmail,
    string? ReviewerName,
    InvitationStatusDto Status,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime? RevokedAtUtc);

public enum InvitationStatusDto
{
    Active = 0,
    Revoked = 1,
    Expired = 2
}

public sealed record ReviewInvitationDetail(
    Guid PublicId,
    Guid Token,
    string ReviewerEmail,
    string? ReviewerName,
    InvitationStatusDto Status,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc,
    string ReviewPath);

public sealed record ReviewCommentDetail(
    Guid PublicId,
    string AuthorName,
    string Content,
    DateTime CreatedAtUtc);

public sealed record ChangeSuggestionDetail(
    Guid PublicId,
    string AuthorName,
    string SectionReference,
    string SuggestedChange,
    DateTime CreatedAtUtc);

public sealed record ApprovalRecordDetail(
    Guid PublicId,
    string ApproverName,
    string ApproverEmail,
    string CanonicalHashSha256,
    string ConfirmationStatement,
    int SnapshotVersionNumber,
    DateTime ApprovedAtUtc);

public sealed record ExternalReviewSnapshotDetail(
    AgreementSnapshotDetail Snapshot,
    IReadOnlyList<ReviewCommentDetail> Comments,
    IReadOnlyList<ChangeSuggestionDetail> ChangeSuggestions);

public sealed record CreateReviewInvitationRequest(
    string ReviewerEmail,
    string? ReviewerName,
    int? ExpirationDays);

public sealed record AddReviewCommentRequest(
    string AuthorName,
    string Content);

public sealed record AddChangeSuggestionRequest(
    string AuthorName,
    string SectionReference,
    string SuggestedChange);

public sealed record ApproveSnapshotRequest(
    string ApproverName,
    string ApproverEmail,
    string ConfirmationStatement);

public interface IReviewApprovalService
{
    Task<(AgreementSnapshotDetail? Snapshot, string? Error)> ShareSnapshotAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<(AgreementSnapshotDetail? Snapshot, string? Error)> MarkReadyForApprovalAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<(ReviewInvitationDetail? Invitation, string? Error)> CreateInvitationAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        Guid userId,
        CreateReviewInvitationRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> RevokeInvitationAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        Guid invitationPublicId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewInvitationSummary>?> ListInvitationsAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        CancellationToken cancellationToken = default);

    Task<ApprovalRecordDetail?> GetApprovalRecordAsync(
        Guid tenantId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        CancellationToken cancellationToken = default);

    Task<ExternalReviewSnapshotDetail?> GetSnapshotForReviewAsync(
        Guid token,
        CancellationToken cancellationToken = default);

    Task<(ReviewCommentDetail? Comment, string? Error)> AddCommentAsync(
        Guid token,
        AddReviewCommentRequest request,
        CancellationToken cancellationToken = default);

    Task<(ChangeSuggestionDetail? Suggestion, string? Error)> AddChangeSuggestionAsync(
        Guid token,
        AddChangeSuggestionRequest request,
        CancellationToken cancellationToken = default);

    Task<(AgreementSnapshotDetail? Snapshot, string? Error)> RequestChangesAsync(
        Guid token,
        CancellationToken cancellationToken = default);

    Task<(ApprovalRecordDetail? Approval, string? Error)> ApproveSnapshotAsync(
        Guid token,
        ApproveSnapshotRequest request,
        CancellationToken cancellationToken = default);
}
