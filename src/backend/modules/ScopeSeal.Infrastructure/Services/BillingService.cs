using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Audit.Services;
using ScopeSeal.Billing.Configuration;
using ScopeSeal.Billing.Domain;
using ScopeSeal.Billing.Services;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Infrastructure.Persistence;

namespace ScopeSeal.Infrastructure.Services;

public sealed class BillingService(
    ApplicationDbContext dbContext,
    PaymentGatewayFactory paymentGatewayFactory,
    IEntitlementService entitlementService,
    IAuditService auditService,
    IOptions<BillingOptions> billingOptions,
    ILogger<BillingService> logger) : IBillingService
{
    private readonly BillingOptions _options = billingOptions.Value;

    public async Task<(CheckoutSessionResponse? Session, string? Error)> CreateCheckoutAsync(
        Guid tenantId,
        Guid userId,
        CreateCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_options.Mode == "Disabled")
        {
            return (null, "Billing is not enabled.");
        }

        if (request.PlanCode == PlanCode.Free)
        {
            return (null, "Free plan does not require checkout.");
        }

        var planId = ResolveRazorpayPlanId(request.PlanCode, request.Interval);
        if (string.IsNullOrWhiteSpace(planId))
        {
            return (null, "Selected plan is not configured for billing.");
        }

        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user?.Email is null)
        {
            return (null, "User email is required for billing.");
        }

        var tenant = await dbContext.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return (null, "Tenant not found.");
        }

        var gateway = paymentGatewayFactory.Resolve();
        var customer = await dbContext.BillingCustomers
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);

        if (customer is null)
        {
            var customerResult = await gateway.CreateOrGetCustomerAsync(
                new PaymentCustomerRequest(user.Email, user.DisplayName, tenant.PublicId.ToString()),
                cancellationToken);
            customer = new BillingCustomer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ExternalCustomerId = customerResult.ExternalCustomerId,
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.BillingCustomers.Add(customer);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var subscriptionResult = await gateway.CreateSubscriptionAsync(
            new PaymentSubscriptionRequest(
                customer.ExternalCustomerId,
                planId,
                new Dictionary<string, string>
                {
                    ["tenant_id"] = tenantId.ToString(),
                    ["plan_code"] = request.PlanCode.ToString(),
                    ["interval"] = request.Interval.ToString()
                }),
            cancellationToken);

        var now = DateTime.UtcNow;
        var subscription = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            PlanCode = request.PlanCode,
            Interval = request.Interval,
            ExternalSubscriptionId = subscriptionResult.ExternalSubscriptionId,
            Status = SubscriptionStatus.Created,
            EntitlementGranted = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.TenantSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.BillingCheckoutCreated,
            "TenantSubscription",
            subscription.PublicId,
            userId,
            $"Checkout created for {subscription.PlanCode} ({subscription.Interval}).",
            cancellationToken);

        return (new CheckoutSessionResponse(
            subscription.PublicId,
            subscription.ExternalSubscriptionId,
            subscriptionResult.CheckoutKeyId,
            customer.ExternalCustomerId,
            planId,
            subscriptionResult.Status), null);
    }

    public async Task<(bool Success, string? Error)> VerifyPaymentAsync(
        Guid tenantId,
        VerifyPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_options.Mode == "Disabled")
        {
            return (false, "Billing is not enabled.");
        }

        var subscription = await dbContext.TenantSubscriptions
            .FirstOrDefaultAsync(
                s => s.TenantId == tenantId && s.ExternalSubscriptionId == request.SubscriptionId,
                cancellationToken);
        if (subscription is null)
        {
            return (false, "Subscription not found.");
        }

        var gateway = paymentGatewayFactory.Resolve();
        var verification = gateway.VerifyPaymentSignature(
            request.SubscriptionId,
            request.PaymentId,
            request.Signature);
        if (!verification.IsValid)
        {
            return (false, verification.FailureReason ?? "Invalid payment signature.");
        }

        subscription.Status = SubscriptionStatus.Pending;
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            tenantId,
            AuditEventType.BillingPaymentVerified,
            "TenantSubscription",
            subscription.PublicId,
            summary: $"Payment {request.PaymentId} verified provisionally.",
            cancellationToken: cancellationToken);

        return (true, null);
    }

    public async Task<(bool Accepted, string? Error)> ProcessWebhookAsync(
        byte[] rawBody,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (_options.Mode == "Disabled")
        {
            return (false, "Billing is not enabled.");
        }

        var gateway = paymentGatewayFactory.Resolve();
        var verification = gateway.VerifyWebhookSignature(rawBody, signature);
        if (!verification.IsValid)
        {
            logger.LogWarning("Rejected Razorpay webhook with invalid signature.");
            return (false, verification.FailureReason ?? "Invalid webhook signature.");
        }

        var rawBodyString = Encoding.UTF8.GetString(rawBody);
        var webhookEvent = gateway.ParseWebhookEvent(rawBodyString);
        var fingerprint = Convert.ToHexString(SHA256.HashData(rawBody)).ToLowerInvariant();

        var existing = await dbContext.ProcessedWebhookEvents
            .FirstOrDefaultAsync(
                e => e.ProviderEventId == webhookEvent.ProviderEventId
                    || e.PayloadFingerprint == fingerprint,
                cancellationToken);
        if (existing is not null)
        {
            return (true, null);
        }

        dbContext.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
        {
            Id = Guid.NewGuid(),
            ProviderEventId = webhookEvent.ProviderEventId,
            EventType = webhookEvent.EventType,
            PayloadFingerprint = fingerprint,
            ProcessedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(webhookEvent.ExternalSubscriptionId))
        {
            return (true, null);
        }

        var subscription = await dbContext.TenantSubscriptions
            .FirstOrDefaultAsync(
                s => s.ExternalSubscriptionId == webhookEvent.ExternalSubscriptionId,
                cancellationToken);
        if (subscription is null)
        {
            logger.LogWarning(
                "Webhook {EventType} referenced unknown subscription {SubscriptionId}",
                webhookEvent.EventType,
                webhookEvent.ExternalSubscriptionId);
            return (true, null);
        }

        await ApplyWebhookEventAsync(subscription, webhookEvent, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelSubscriptionAsync(
        Guid tenantId,
        bool cancelAtCycleEnd,
        CancellationToken cancellationToken = default)
    {
        var subscription = await GetActiveSubscriptionAsync(tenantId, cancellationToken);
        if (subscription is null)
        {
            return (false, "No active subscription found.");
        }

        var gateway = paymentGatewayFactory.Resolve();
        await gateway.CancelSubscriptionAsync(subscription.ExternalSubscriptionId, cancelAtCycleEnd, cancellationToken);

        subscription.Status = cancelAtCycleEnd ? SubscriptionStatus.Active : SubscriptionStatus.Cancelled;
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        if (!cancelAtCycleEnd)
        {
            subscription.EntitlementGranted = false;
            await entitlementService.AssignPlanAsync(tenantId, PlanCode.Free, EntitlementSource.FreePlan, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(
            tenantId,
            AuditEventType.BillingSubscriptionCancelled,
            "TenantSubscription",
            subscription.PublicId,
            summary: cancelAtCycleEnd ? "Subscription scheduled for cancellation." : "Subscription cancelled immediately.",
            cancellationToken: cancellationToken);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePlanAsync(
        Guid tenantId,
        ChangePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PlanCode == PlanCode.Free)
        {
            return await CancelSubscriptionAsync(tenantId, cancelAtCycleEnd: false, cancellationToken);
        }

        var current = await GetActiveSubscriptionAsync(tenantId, cancellationToken);
        if (current is not null &&
            current.PlanCode == request.PlanCode &&
            current.Interval == request.Interval &&
            current.Status is SubscriptionStatus.Active or SubscriptionStatus.Pending or SubscriptionStatus.Authenticated)
        {
            return (true, null);
        }

        if (current is not null)
        {
            var cancelResult = await CancelSubscriptionAsync(tenantId, cancelAtCycleEnd: false, cancellationToken);
            if (!cancelResult.Success)
            {
                return cancelResult;
            }
        }

        return (false, "Plan changes require a new checkout session.");
    }

    public async Task<int> ReconcilePendingSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        if (_options.Mode == "Disabled")
        {
            return 0;
        }

        var gateway = paymentGatewayFactory.Resolve();
        var pending = await dbContext.TenantSubscriptions
            .Where(s => !s.EntitlementGranted
                && (s.Status == SubscriptionStatus.Pending
                    || s.Status == SubscriptionStatus.Authenticated
                    || s.Status == SubscriptionStatus.Created))
            .ToListAsync(cancellationToken);

        var reconciled = 0;
        foreach (var subscription in pending)
        {
            var remote = await gateway.GetSubscriptionAsync(subscription.ExternalSubscriptionId, cancellationToken);
            if (remote is null)
            {
                continue;
            }

            if (remote.Status.Equals("active", StringComparison.OrdinalIgnoreCase)
                || remote.Status.Equals("authenticated", StringComparison.OrdinalIgnoreCase))
            {
                subscription.Status = SubscriptionStatus.Active;
                subscription.UpdatedAtUtc = DateTime.UtcNow;
                await GrantEntitlementIfNeededAsync(subscription, cancellationToken);
                reconciled++;
            }
        }

        if (reconciled > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return reconciled;
    }

    public async Task<BillingStatusResponse?> GetBillingStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var entitlement = await entitlementService.GetSummaryAsync(tenantId, cancellationToken);
        if (entitlement is null)
        {
            return null;
        }

        var subscription = await dbContext.TenantSubscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new BillingStatusResponse(
            entitlement.PlanCode,
            subscription?.Interval,
            subscription?.Status,
            entitlement.Source,
            subscription?.EntitlementGranted ?? false,
            subscription?.GracePeriodEndsAtUtc,
            subscription?.ExternalSubscriptionId);
    }

    private async Task ApplyWebhookEventAsync(
        TenantSubscription subscription,
        PaymentWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        switch (webhookEvent.EventType)
        {
            case "subscription.authenticated":
            case "subscription.activated":
            case "subscription.charged":
            case "subscription.resumed":
                subscription.Status = SubscriptionStatus.Active;
                subscription.GracePeriodEndsAtUtc = null;
                subscription.UpdatedAtUtc = now;
                await GrantEntitlementIfNeededAsync(subscription, cancellationToken);
                break;
            case "subscription.pending":
                subscription.Status = SubscriptionStatus.Pending;
                subscription.UpdatedAtUtc = now;
                break;
            case "subscription.halted":
            case "payment.failed":
                subscription.Status = SubscriptionStatus.GracePeriod;
                subscription.GracePeriodEndsAtUtc = now.AddDays(_options.FailedPaymentGracePeriodDays);
                subscription.UpdatedAtUtc = now;
                break;
            case "subscription.cancelled":
            case "subscription.completed":
                subscription.Status = webhookEvent.MappedStatus ?? SubscriptionStatus.Cancelled;
                subscription.EntitlementGranted = false;
                subscription.GracePeriodEndsAtUtc = null;
                subscription.UpdatedAtUtc = now;
                await entitlementService.AssignPlanAsync(
                    subscription.TenantId,
                    PlanCode.Free,
                    EntitlementSource.FreePlan,
                    cancellationToken);
                break;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.RecordAsync(
            subscription.TenantId,
            AuditEventType.BillingWebhookProcessed,
            "TenantSubscription",
            subscription.PublicId,
            summary: $"Processed webhook {webhookEvent.EventType}.",
            cancellationToken: cancellationToken);
    }

    private async Task GrantEntitlementIfNeededAsync(
        TenantSubscription subscription,
        CancellationToken cancellationToken)
    {
        if (subscription.EntitlementGranted)
        {
            return;
        }

        await entitlementService.AssignPlanAsync(
            subscription.TenantId,
            subscription.PlanCode,
            EntitlementSource.WebSubscription,
            cancellationToken);

        subscription.EntitlementGranted = true;
        subscription.Status = SubscriptionStatus.Active;
        subscription.GracePeriodEndsAtUtc = null;
        subscription.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            subscription.TenantId,
            AuditEventType.BillingEntitlementGranted,
            "TenantSubscription",
            subscription.PublicId,
            summary: $"Granted {subscription.PlanCode} entitlements from verified subscription.",
            cancellationToken: cancellationToken);
    }

    private async Task<TenantSubscription?> GetActiveSubscriptionAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        await dbContext.TenantSubscriptions
            .Where(s => s.TenantId == tenantId
                && s.Status != SubscriptionStatus.Cancelled
                && s.Status != SubscriptionStatus.Completed)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    private string ResolveRazorpayPlanId(PlanCode planCode, BillingInterval interval) =>
        (planCode, interval) switch
        {
            (PlanCode.Pro, BillingInterval.Monthly) => _options.Plans.Pro.MonthlyRazorpayPlanId,
            (PlanCode.Pro, BillingInterval.Annual) => _options.Plans.Pro.AnnualRazorpayPlanId,
            (PlanCode.Business, BillingInterval.Monthly) => _options.Plans.Business.MonthlyRazorpayPlanId,
            (PlanCode.Business, BillingInterval.Annual) => _options.Plans.Business.AnnualRazorpayPlanId,
            _ => string.Empty
        };
}
