using ScopeSeal.Billing.Domain;
using ScopeSeal.Entitlements.Domain;

namespace ScopeSeal.Billing.Services;

public interface IPaymentGateway
{
    Task<PaymentCustomerResult> CreateOrGetCustomerAsync(
        PaymentCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentSubscriptionResult> CreateSubscriptionAsync(
        PaymentSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    PaymentSignatureVerificationResult VerifyPaymentSignature(
        string subscriptionId,
        string paymentId,
        string signature);

    PaymentSignatureVerificationResult VerifyWebhookSignature(
        ReadOnlySpan<byte> rawBody,
        string signature);

    PaymentWebhookEvent ParseWebhookEvent(string rawBody);

    Task CancelSubscriptionAsync(
        string externalSubscriptionId,
        bool cancelAtCycleEnd,
        CancellationToken cancellationToken = default);

    Task<PaymentSubscriptionSnapshot?> GetSubscriptionAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken = default);
}

public interface IBillingService
{
    Task<(CheckoutSessionResponse? Session, string? Error)> CreateCheckoutAsync(
        Guid tenantId,
        Guid userId,
        CreateCheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> VerifyPaymentAsync(
        Guid tenantId,
        VerifyPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Accepted, string? Error)> ProcessWebhookAsync(
        byte[] rawBody,
        string signature,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> CancelSubscriptionAsync(
        Guid tenantId,
        bool cancelAtCycleEnd,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> ChangePlanAsync(
        Guid tenantId,
        ChangePlanRequest request,
        CancellationToken cancellationToken = default);

    Task<int> ReconcilePendingSubscriptionsAsync(CancellationToken cancellationToken = default);

    Task<BillingStatusResponse?> GetBillingStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record CreateCheckoutRequest(PlanCode PlanCode, BillingInterval Interval);

public sealed record VerifyPaymentRequest(
    string SubscriptionId,
    string PaymentId,
    string Signature);

public sealed record ChangePlanRequest(PlanCode PlanCode, BillingInterval Interval);

public sealed record CheckoutSessionResponse(
    Guid SubscriptionPublicId,
    string ExternalSubscriptionId,
    string CheckoutKeyId,
    string CustomerId,
    string PlanId,
    string Status);

public sealed record BillingStatusResponse(
    PlanCode PlanCode,
    BillingInterval? Interval,
    SubscriptionStatus? SubscriptionStatus,
    EntitlementSource EntitlementSource,
    bool EntitlementGranted,
    DateTime? GracePeriodEndsAtUtc,
    string? ExternalSubscriptionId);

public sealed record PaymentCustomerRequest(
    string Email,
    string Name,
    string TenantReference);

public sealed record PaymentCustomerResult(string ExternalCustomerId);

public sealed record PaymentSubscriptionRequest(
    string ExternalCustomerId,
    string RazorpayPlanId,
    Dictionary<string, string> Notes);

public sealed record PaymentSubscriptionResult(
    string ExternalSubscriptionId,
    string Status,
    string CheckoutKeyId);

public sealed record PaymentSignatureVerificationResult(bool IsValid, string? FailureReason);

public sealed record PaymentWebhookEvent(
    string ProviderEventId,
    string EventType,
    string? ExternalSubscriptionId,
    string? ExternalPaymentId,
    SubscriptionStatus? MappedStatus);

public sealed record PaymentSubscriptionSnapshot(
    string ExternalSubscriptionId,
    string Status,
    string? PlanId);
