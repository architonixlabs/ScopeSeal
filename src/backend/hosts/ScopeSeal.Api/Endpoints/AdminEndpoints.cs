using Microsoft.Extensions.Options;
using ScopeSeal.Administration.Configuration;
using ScopeSeal.Administration.Services;
using ScopeSeal.Api.Authorization;

namespace ScopeSeal.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Administration");

        group.MapGet("/tenants/search", SearchTenantsAsync)
            .WithName("AdminSearchTenants");

        group.MapGet("/tenants/{tenantPublicId:guid}/inspection", GetTenantInspectionAsync)
            .WithName("AdminGetTenantInspection");

        group.MapGet("/billing/events", ListBillingEventsAsync)
            .WithName("AdminListBillingEvents");

        group.MapGet("/jobs/failed", ListFailedJobsAsync)
            .WithName("AdminListFailedJobs");

        group.MapGet("/jobs/dead-letter", ListDeadLetterJobsAsync)
            .WithName("AdminListDeadLetterJobs");

        group.MapPost("/jobs/dead-letter/sync", SyncDeadLetterJobsAsync)
            .WithName("AdminSyncDeadLetterJobs");

        group.MapPost("/jobs/dead-letter/{deadLetterPublicId:guid}/requeue", RequeueDeadLetterJobAsync)
            .WithName("AdminRequeueDeadLetterJob");

        group.MapGet("/privacy/grievances", ListGrievanceQueueAsync)
            .WithName("AdminListGrievanceQueue");

        group.MapGet("/feature-flags", ListFeatureFlagsAsync)
            .WithName("AdminListFeatureFlags");

        group.MapPut("/feature-flags/{key}", UpdateFeatureFlagAsync)
            .WithName("AdminUpdateFeatureFlag");

        group.MapGet("/notices/privacy", ListPrivacyNoticeVersionsAsync)
            .WithName("AdminListPrivacyNoticeVersions");

        group.MapGet("/notices/terms", ListTermsNoticeVersionsAsync)
            .WithName("AdminListTermsNoticeVersions");

        group.MapPost("/notices/terms", CreateTermsNoticeVersionAsync)
            .WithName("AdminCreateTermsNoticeVersion");

        group.MapGet("/support-access/grants", ListSupportAccessGrantsAsync)
            .WithName("AdminListSupportAccessGrants");

        group.MapPost("/support-access/grants", CreateSupportAccessGrantAsync)
            .WithName("AdminCreateSupportAccessGrant");

        group.MapPost("/support-access/grants/{grantPublicId:guid}/revoke", RevokeSupportAccessGrantAsync)
            .WithName("AdminRevokeSupportAccessGrant");

        group.MapGet("/audit/events", ListAuditEventsAsync)
            .WithName("AdminListAuditEvents");

        return app;
    }

    private static async Task<IResult> SearchTenantsAsync(
        string? q,
        int? limit,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var items = await administrationService.SearchTenantsAsync(q, limit, cancellationToken);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> GetTenantInspectionAsync(
        Guid tenantPublicId,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var summary = await administrationService.GetTenantInspectionAsync(tenantPublicId, cancellationToken);
        if (summary is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(summary);
    }

    private static async Task<IResult> ListBillingEventsAsync(
        Guid? tenantPublicId,
        int? limit,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var items = await administrationService.ListBillingEventsAsync(
            tenantPublicId,
            limit ?? 50,
            cancellationToken);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> ListFailedJobsAsync(
        Guid? tenantPublicId,
        int? limit,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var items = await administrationService.ListFailedJobsAsync(
            tenantPublicId,
            limit ?? 50,
            cancellationToken);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> ListDeadLetterJobsAsync(
        int? limit,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var items = await administrationService.ListDeadLetterJobsAsync(limit ?? 50, cancellationToken);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> SyncDeadLetterJobsAsync(
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var added = await administrationService.SyncDeadLetterFromFailedJobsAsync(cancellationToken);
        return Results.Ok(new { added });
    }

    private static async Task<IResult> RequeueDeadLetterJobAsync(
        Guid deadLetterPublicId,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var (item, error) = await administrationService.RequeueDeadLetterJobAsync(
            deadLetterPublicId,
            cancellationToken);

        if (item is null)
        {
            return Results.Problem(
                title: "Requeue failed",
                detail: error,
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(item);
    }

    private static async Task<IResult> ListGrievanceQueueAsync(
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var items = await administrationService.ListGrievanceQueueAsync(cancellationToken);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> ListFeatureFlagsAsync(
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var items = await administrationService.ListFeatureFlagsAsync(cancellationToken);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> UpdateFeatureFlagAsync(
        string key,
        UpdateFeatureFlagRequest request,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var (item, error) = await administrationService.UpdateFeatureFlagAsync(key, request, cancellationToken);
        if (item is null)
        {
            return Results.Problem(
                title: "Feature flag update failed",
                detail: error,
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(item);
    }

    private static async Task<IResult> ListPrivacyNoticeVersionsAsync(
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var items = await administrationService.ListPrivacyNoticeVersionsAsync(cancellationToken);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> ListTermsNoticeVersionsAsync(
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var items = await administrationService.ListTermsNoticeVersionsAsync(cancellationToken);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> CreateTermsNoticeVersionAsync(
        CreateNoticeVersionRequest request,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var (item, error) = await administrationService.CreateTermsNoticeVersionAsync(request, cancellationToken);
        if (item is null)
        {
            return Results.Problem(
                title: "Terms notice creation failed",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Created($"/api/v1/admin/notices/terms/{item.PublicId}", item);
    }

    private static async Task<IResult> ListSupportAccessGrantsAsync(
        Guid? tenantPublicId,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var items = await administrationService.ListSupportAccessGrantsAsync(tenantPublicId, cancellationToken);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> CreateSupportAccessGrantAsync(
        CreateSupportAccessGrantRequest request,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var (item, error) = await administrationService.CreateSupportAccessGrantAsync(request, cancellationToken);
        if (item is null)
        {
            return Results.Problem(
                title: "Support access grant failed",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Created($"/api/v1/admin/support-access/grants/{item.PublicId}", item);
    }

    private static async Task<IResult> RevokeSupportAccessGrantAsync(
        Guid grantPublicId,
        RevokeSupportAccessGrantRequest request,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var (item, error) = await administrationService.RevokeSupportAccessGrantAsync(
            grantPublicId,
            request,
            cancellationToken);

        if (item is null)
        {
            return Results.Problem(
                title: "Support access revoke failed",
                detail: error,
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(item);
    }

    private static async Task<IResult> ListAuditEventsAsync(
        Guid? tenantPublicId,
        string? eventType,
        DateTime? fromUtc,
        DateTime? toUtc,
        int? limit,
        HttpRequest httpRequest,
        IAdministrationService administrationService,
        IOptions<AdministrationOptions> administrationOptions,
        CancellationToken cancellationToken)
    {
        if (!Authorize(httpRequest, administrationOptions.Value))
        {
            return Results.Unauthorized();
        }

        var items = await administrationService.ListAuditEventsAsync(
            new AuditEventQuery(tenantPublicId, eventType, fromUtc, toUtc, limit ?? 50),
            cancellationToken);
        return Results.Ok(new { items });
    }

    private static bool Authorize(HttpRequest request, AdministrationOptions options) =>
        AdminOperatorAuth.IsAuthorized(request, options);
}
