using System.Security.Claims;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Privacy.Domain;
using ScopeSeal.Privacy.Services;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class PrivacyEndpoints
{
    public static IEndpointRouteBuilder MapPrivacyEndpoints(this IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup("/api/v1/privacy").WithTags("Privacy");

        publicGroup.MapGet("/notices/current", GetCurrentNoticeAsync)
            .WithName("GetCurrentPrivacyNotice")
            .AllowAnonymous();

        publicGroup.MapGet("/notices/{noticePublicId:guid}", GetNoticeAsync)
            .WithName("GetPrivacyNotice")
            .AllowAnonymous();

        publicGroup.MapGet("/subprocessors", ListSubprocessorsAsync)
            .WithName("ListSubprocessors")
            .AllowAnonymous();

        var tenantGroup = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/privacy")
            .WithTags("Privacy");

        tenantGroup.MapGet("/summary", GetSummaryAsync)
            .WithName("GetPrivacyCentreSummary")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        tenantGroup.MapGet("/consents", ListConsentsAsync)
            .WithName("ListPrivacyConsents")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        tenantGroup.MapPost("/consents", RecordConsentsAsync)
            .WithName("RecordPrivacyConsents")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        tenantGroup.MapPost("/consents/{consentPublicId:guid}/withdraw", WithdrawConsentAsync)
            .WithName("WithdrawPrivacyConsent")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        tenantGroup.MapGet("/requests", ListRequestsAsync)
            .WithName("ListPrivacyRequests")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        tenantGroup.MapPost("/requests", SubmitRequestAsync)
            .WithName("SubmitPrivacyRequest")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        tenantGroup.MapGet("/requests/{requestPublicId:guid}", GetRequestAsync)
            .WithName("GetPrivacyRequest")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        return app;
    }

    private static async Task<IResult> GetCurrentNoticeAsync(
        IPrivacyService privacyService,
        CancellationToken cancellationToken)
    {
        var notice = await privacyService.GetCurrentNoticeAsync(cancellationToken);
        return notice is null ? Results.NotFound() : Results.Ok(notice);
    }

    private static async Task<IResult> GetNoticeAsync(
        Guid noticePublicId,
        IPrivacyService privacyService,
        CancellationToken cancellationToken)
    {
        var notice = await privacyService.GetNoticeAsync(noticePublicId, cancellationToken);
        return notice is null ? Results.NotFound() : Results.Ok(notice);
    }

    private static async Task<IResult> ListSubprocessorsAsync(
        IPrivacyService privacyService,
        CancellationToken cancellationToken)
    {
        var subprocessors = await privacyService.ListSubprocessorsAsync(cancellationToken);
        return Results.Ok(new { subprocessors });
    }

    private static async Task<IResult> GetSummaryAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IPrivacyService privacyService,
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

        var summary = await privacyService.GetPrivacyCentreSummaryAsync(
            tenant.TenantId,
            userId.Value,
            cancellationToken);

        return summary is null ? Results.NotFound() : Results.Ok(summary);
    }

    private static async Task<IResult> ListConsentsAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IPrivacyService privacyService,
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

        var consents = await privacyService.ListConsentsAsync(
            tenant.TenantId,
            userId.Value,
            cancellationToken);

        return Results.Ok(new { consents });
    }

    private static async Task<IResult> RecordConsentsAsync(
        Guid tenantPublicId,
        RecordConsentsRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IPrivacyService privacyService,
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

        var (consents, error) = await privacyService.RecordConsentsAsync(
            tenant.TenantId,
            userId.Value,
            request,
            cancellationToken);

        if (error is not null)
        {
            return Results.Problem(
                title: "Consent recording failed",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { consents });
    }

    private static async Task<IResult> WithdrawConsentAsync(
        Guid tenantPublicId,
        Guid consentPublicId,
        WithdrawConsentRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IPrivacyService privacyService,
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

        var (consent, error) = await privacyService.WithdrawConsentAsync(
            tenant.TenantId,
            userId.Value,
            consentPublicId,
            request.Reason,
            cancellationToken);

        if (consent is null)
        {
            return Results.Problem(
                title: "Consent withdrawal failed",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(consent);
    }

    private static async Task<IResult> ListRequestsAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IPrivacyService privacyService,
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

        var requests = await privacyService.ListRequestsAsync(
            tenant.TenantId,
            userId.Value,
            cancellationToken);

        return Results.Ok(new { requests });
    }

    private static async Task<IResult> SubmitRequestAsync(
        Guid tenantPublicId,
        SubmitPrivacyRequestDto request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IPrivacyService privacyService,
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

        var (privacyRequest, error) = await privacyService.SubmitRequestAsync(
            tenant.TenantId,
            userId.Value,
            new SubmitPrivacyRequest(
                request.RequestType,
                request.Subject,
                request.Details,
                request.CorrectionDetails,
                request.GrievanceCategory),
            cancellationToken);

        if (privacyRequest is null)
        {
            return Results.Problem(
                title: "Privacy request failed",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Created(
            $"/api/v1/tenants/{tenantPublicId}/privacy/requests/{privacyRequest.PublicId}",
            privacyRequest);
    }

    private static async Task<IResult> GetRequestAsync(
        Guid tenantPublicId,
        Guid requestPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IPrivacyService privacyService,
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

        var request = await privacyService.GetRequestAsync(
            tenant.TenantId,
            userId.Value,
            requestPublicId,
            cancellationToken);

        return request is null ? Results.NotFound() : Results.Ok(request);
    }

    private sealed record WithdrawConsentRequest(string? Reason);

    private sealed record SubmitPrivacyRequestDto(
        PrivacyRequestType RequestType,
        string Subject,
        string Details,
        string? CorrectionDetails,
        string? GrievanceCategory);
}
