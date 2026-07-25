using ScopeSeal.Privacy.Domain;

namespace ScopeSeal.Privacy.Services;

public interface IPrivacyService
{
    Task<PrivacyCentreSummaryResponse?> GetPrivacyCentreSummaryAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PrivacyNoticeResponse?> GetCurrentNoticeAsync(CancellationToken cancellationToken = default);

    Task<PrivacyNoticeResponse?> GetNoticeAsync(
        Guid noticePublicId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ConsentRecordResponse> Consents, string? Error)> RecordConsentsAsync(
        Guid tenantId,
        Guid userId,
        RecordConsentsRequest request,
        CancellationToken cancellationToken = default);

    Task<(ConsentRecordResponse? Consent, string? Error)> WithdrawConsentAsync(
        Guid tenantId,
        Guid userId,
        Guid consentPublicId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConsentRecordResponse>> ListConsentsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<(PrivacyRequestResponse? Request, string? Error)> SubmitRequestAsync(
        Guid tenantId,
        Guid userId,
        SubmitPrivacyRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrivacyRequestResponse>> ListRequestsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PrivacyRequestResponse?> GetRequestAsync(
        Guid tenantId,
        Guid userId,
        Guid requestPublicId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubprocessorResponse>> ListSubprocessorsAsync(
        CancellationToken cancellationToken = default);

    Task<int> ProcessPendingPrivacyJobsAsync(CancellationToken cancellationToken = default);

    Task<int> RunRetentionFoundationJobAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminPrivacyQueueItemResponse>> ListAdminQueueAsync(
        CancellationToken cancellationToken = default);

    Task<(AdminPrivacyQueueItemResponse? Item, string? Error)> UpdateAdminQueueItemAsync(
        Guid queuePublicId,
        UpdateAdminQueueItemRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record RecordConsentsRequest(
    Guid NoticePublicId,
    bool RequiredTermsAccepted,
    bool? OptionalMarketingAccepted,
    bool? OptionalAnalyticsAccepted);

public sealed record SubmitPrivacyRequest(
    PrivacyRequestType RequestType,
    string Subject,
    string Details,
    string? CorrectionDetails,
    string? GrievanceCategory);

public sealed record UpdateAdminQueueItemRequest(
    AdminQueueStatus? QueueStatus,
    string? AssignedOperator,
    string? Notes);

public sealed record PrivacyCentreSummaryResponse(
    Guid TenantPublicId,
    PrivacyNoticeResponse? CurrentNotice,
    IReadOnlyList<ConsentRecordResponse> ActiveConsents,
    IReadOnlyList<PrivacyRequestResponse> OpenRequests,
    IReadOnlyList<DataExportJobResponse> ExportJobs,
    IReadOnlyList<DeletionJobResponse> DeletionJobs,
    IReadOnlyList<SubprocessorResponse> Subprocessors);

public sealed record PrivacyNoticeResponse(
    Guid PublicId,
    string Version,
    string Title,
    string Summary,
    DateTime EffectiveFromUtc,
    bool IsCurrent);

public sealed record ConsentRecordResponse(
    Guid PublicId,
    ConsentType ConsentType,
    string Purpose,
    bool Granted,
    DateTime GrantedAtUtc,
    DateTime? WithdrawnAtUtc,
    Guid NoticePublicId);

public sealed record PrivacyRequestResponse(
    Guid PublicId,
    PrivacyRequestType RequestType,
    PrivacyRequestStatus Status,
    string Subject,
    string Details,
    string? CorrectionDetails,
    string? GrievanceCategory,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    string? StatusMessage);

public sealed record DataExportJobResponse(
    Guid PublicId,
    ExportJobStatus Status,
    string? DownloadToken,
    DateTime? ExpiresAtUtc,
    DateTime CreatedAtUtc);

public sealed record DeletionJobResponse(
    Guid PublicId,
    DeletionJobStatus Status,
    DeletionStep CurrentStep,
    DateTime ScheduledBackupPurgeAtUtc,
    string StatusMessage);

public sealed record SubprocessorResponse(
    Guid PublicId,
    string Name,
    string Purpose,
    string DataProcessed,
    string Location,
    string ContractStatus,
    string DpaStatus);

public sealed record AdminPrivacyQueueItemResponse(
    Guid PublicId,
    Guid RequestPublicId,
    PrivacyRequestType RequestType,
    AdminQueueStatus QueueStatus,
    string? AssignedOperator,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
