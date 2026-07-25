using ScopeSeal.Administration.Domain;

namespace ScopeSeal.Administration.Services;

public interface IAdministrationService
{
    Task<IReadOnlyList<TenantMetadataSummary>> SearchTenantsAsync(
        string? query,
        int? limit,
        CancellationToken cancellationToken = default);

    Task<TenantInspectionSummary?> GetTenantInspectionAsync(
        Guid tenantPublicId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BillingEventSummary>> ListBillingEventsAsync(
        Guid? tenantPublicId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FailedJobSummary>> ListFailedJobsAsync(
        Guid? tenantPublicId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeadLetterJobSummary>> ListDeadLetterJobsAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> SyncDeadLetterFromFailedJobsAsync(CancellationToken cancellationToken = default);

    Task<(DeadLetterJobSummary? Item, string? Error)> RequeueDeadLetterJobAsync(
        Guid deadLetterPublicId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GrievanceQueueItemSummary>> ListGrievanceQueueAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeatureFlagSummary>> ListFeatureFlagsAsync(
        CancellationToken cancellationToken = default);

    Task<(FeatureFlagSummary? Item, string? Error)> UpdateFeatureFlagAsync(
        string key,
        UpdateFeatureFlagRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NoticeVersionSummary>> ListPrivacyNoticeVersionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NoticeVersionSummary>> ListTermsNoticeVersionsAsync(
        CancellationToken cancellationToken = default);

    Task<(NoticeVersionSummary? Item, string? Error)> CreateTermsNoticeVersionAsync(
        CreateNoticeVersionRequest request,
        CancellationToken cancellationToken = default);

    Task<(SupportAccessGrantSummary? Item, string? Error)> CreateSupportAccessGrantAsync(
        CreateSupportAccessGrantRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportAccessGrantSummary>> ListSupportAccessGrantsAsync(
        Guid? tenantPublicId,
        CancellationToken cancellationToken = default);

    Task<(SupportAccessGrantSummary? Item, string? Error)> RevokeSupportAccessGrantAsync(
        Guid grantPublicId,
        RevokeSupportAccessGrantRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEventSummary>> ListAuditEventsAsync(
        AuditEventQuery query,
        CancellationToken cancellationToken = default);
}

public sealed record TenantMetadataSummary(
    Guid PublicId,
    string Name,
    DateTime CreatedAtUtc,
    int MemberCount,
    string CurrentPlanCode);

public sealed record TenantInspectionSummary(
    Guid PublicId,
    string Name,
    DateTime CreatedAtUtc,
    int MemberCount,
    string CurrentPlanCode,
    string EntitlementSource,
    int ActiveWorkspaceCount,
    int OpenPrivacyRequestCount,
    SubscriptionInspectionSummary? Subscription);

public sealed record SubscriptionInspectionSummary(
    Guid PublicId,
    string PlanCode,
    string Interval,
    string Status,
    bool EntitlementGranted,
    DateTime? GracePeriodEndsAtUtc);

public sealed record BillingEventSummary(
    Guid? TenantPublicId,
    string EventType,
    string Summary,
    DateTime OccurredAtUtc,
    string Source);

public sealed record FailedJobSummary(
    Guid PublicId,
    Guid TenantPublicId,
    string JobCategory,
    string Status,
    string? ErrorMessage,
    DateTime CreatedAtUtc);

public sealed record DeadLetterJobSummary(
    Guid PublicId,
    Guid TenantPublicId,
    string JobCategory,
    Guid SourceJobPublicId,
    string ErrorMessage,
    DateTime FailedAtUtc,
    DeadLetterStatus Status,
    DateTime? RequeuedAtUtc);

public sealed record GrievanceQueueItemSummary(
    Guid RequestPublicId,
    Guid TenantPublicId,
    string TenantName,
    string Subject,
    string Status,
    string? GrievanceCategory,
    DateTime CreatedAtUtc);

public sealed record FeatureFlagSummary(
    string Key,
    bool IsEnabled,
    string Description,
    DateTime UpdatedAtUtc);

public sealed record NoticeVersionSummary(
    Guid PublicId,
    string Version,
    string Title,
    string Summary,
    DateTime EffectiveFromUtc,
    bool IsCurrent);

public sealed record SupportAccessGrantSummary(
    Guid PublicId,
    Guid TenantPublicId,
    string TenantName,
    SupportAccessScope Scope,
    string OperatorReference,
    string Reason,
    DateTime GrantedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? RevokedAtUtc,
    bool IsActive);

public sealed record AuditEventSummary(
    Guid TenantPublicId,
    string EventType,
    string EntityType,
    Guid EntityPublicId,
    string? Summary,
    DateTime OccurredAtUtc);

public sealed record UpdateFeatureFlagRequest(bool IsEnabled, string? Description);

public sealed record CreateNoticeVersionRequest(
    string Version,
    string Title,
    string Summary,
    DateTime EffectiveFromUtc,
    bool SetAsCurrent);

public sealed record CreateSupportAccessGrantRequest(
    Guid TenantPublicId,
    string OperatorReference,
    string Reason,
    int? DurationHours);

public sealed record RevokeSupportAccessGrantRequest(string Reason);

public sealed record AuditEventQuery(
    Guid? TenantPublicId,
    string? EventType,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Limit = 50);
