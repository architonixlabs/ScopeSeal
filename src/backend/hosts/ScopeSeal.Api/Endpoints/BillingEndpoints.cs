using System.Security.Claims;
using ScopeSeal.Billing.Domain;
using ScopeSeal.Billing.Services;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/billing")
            .WithTags("Billing");

        group.MapPost("/checkout", CreateCheckoutAsync)
            .WithName("CreateBillingCheckout")
            .RequireAuthorization(ScopeSealPolicies.TenantOwner)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/verify-payment", VerifyPaymentAsync)
            .WithName("VerifyBillingPayment")
            .RequireAuthorization(ScopeSealPolicies.TenantOwner)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/status", GetBillingStatusAsync)
            .WithName("GetBillingStatus")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapPost("/cancel", CancelSubscriptionAsync)
            .WithName("CancelBillingSubscription")
            .RequireAuthorization(ScopeSealPolicies.TenantOwner);

        group.MapPost("/change-plan", ChangePlanAsync)
            .WithName("ChangeBillingPlan")
            .RequireAuthorization(ScopeSealPolicies.TenantOwner);

        return app;
    }

    public static IEndpointRouteBuilder MapRazorpayWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/webhooks/razorpay", ProcessWebhookAsync)
            .WithName("RazorpayWebhook")
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithTags("Billing Webhooks");

        return app;
    }

    private static async Task<IResult> CreateCheckoutAsync(
        Guid tenantPublicId,
        CreateCheckoutRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IBillingService billingService,
        CancellationToken cancellationToken)
    {
        var userId = TenantEndpointHelpers.GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var (session, error) = await billingService.CreateCheckoutAsync(
            tenant.TenantId,
            userId.Value,
            request,
            cancellationToken);

        if (session is null)
        {
            return Results.Problem(
                title: "Checkout unavailable",
                detail: error,
                statusCode: StatusCodes.Status403Forbidden);
        }

        return Results.Created(
            $"/api/v1/tenants/{tenantPublicId}/billing/status",
            new
            {
                subscriptionPublicId = session.SubscriptionPublicId,
                externalSubscriptionId = session.ExternalSubscriptionId,
                checkoutKeyId = session.CheckoutKeyId,
                customerId = session.CustomerId,
                planId = session.PlanId,
                status = session.Status
            });
    }

    private static async Task<IResult> VerifyPaymentAsync(
        Guid tenantPublicId,
        VerifyPaymentRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IBillingService billingService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var (success, error) = await billingService.VerifyPaymentAsync(
            tenant.TenantId,
            request,
            cancellationToken);

        if (!success)
        {
            return Results.Problem(
                title: "Payment verification failed",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { verified = true, provisional = true });
    }

    private static async Task<IResult> GetBillingStatusAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IBillingService billingService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var status = await billingService.GetBillingStatusAsync(tenant.TenantId, cancellationToken);
        if (status is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            plan = status.PlanCode.ToString(),
            interval = status.Interval?.ToString(),
            subscriptionStatus = status.SubscriptionStatus?.ToString(),
            source = status.EntitlementSource.ToString(),
            entitlementGranted = status.EntitlementGranted,
            gracePeriodEndsAtUtc = status.GracePeriodEndsAtUtc,
            externalSubscriptionId = status.ExternalSubscriptionId
        });
    }

    private static async Task<IResult> CancelSubscriptionAsync(
        Guid tenantPublicId,
        CancelSubscriptionRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IBillingService billingService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var (success, error) = await billingService.CancelSubscriptionAsync(
            tenant.TenantId,
            request.CancelAtCycleEnd,
            cancellationToken);

        if (!success)
        {
            return Results.Problem(
                title: "Cancellation failed",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { cancelled = true, atCycleEnd = request.CancelAtCycleEnd });
    }

    private static async Task<IResult> ChangePlanAsync(
        Guid tenantPublicId,
        ChangePlanRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IBillingService billingService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var (success, error) = await billingService.ChangePlanAsync(
            tenant.TenantId,
            request,
            cancellationToken);

        if (!success)
        {
            return Results.Problem(
                title: "Plan change unavailable",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { changed = true });
    }

    private static async Task<IResult> ProcessWebhookAsync(
        HttpRequest request,
        IBillingService billingService,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await request.Body.CopyToAsync(memoryStream, cancellationToken);
        var rawBody = memoryStream.ToArray();

        if (!request.Headers.TryGetValue("X-Razorpay-Signature", out var signatureValues))
        {
            return Results.BadRequest(new { error = "Missing webhook signature." });
        }

        var signature = signatureValues.ToString();
        var (accepted, error) = await billingService.ProcessWebhookAsync(rawBody, signature, cancellationToken);
        if (!accepted)
        {
            return Results.Problem(
                title: "Invalid webhook signature",
                detail: error,
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(new { received = true });
    }

    private sealed record CancelSubscriptionRequest(bool CancelAtCycleEnd);
}
