using System.Security.Claims;
using ScopeSeal.Extraction.Domain;
using ScopeSeal.Extraction.Services;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class ExtractionEndpoints
{
    public static IEndpointRouteBuilder MapExtractionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/workspaces/{workspacePublicId:guid}")
            .WithTags("AI Extraction");

        group.MapPost("/documents/{documentPublicId:guid}/extraction-runs", CreateExtractionRunAsync)
            .WithName("CreateExtractionRun")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/extraction-runs/{extractionRunPublicId:guid}", GetExtractionRunAsync)
            .WithName("GetExtractionRun")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapPost("/extraction-runs/{extractionRunPublicId:guid}/facts/{factPublicId:guid}/review", ReviewFactAsync)
            .WithName("ReviewExtractedFact")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/extraction-runs/{extractionRunPublicId:guid}/apply/{snapshotPublicId:guid}", ApplyAcceptedFactsAsync)
            .WithName("ApplyAcceptedExtractionFacts")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> CreateExtractionRunAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid documentPublicId,
        CreateExtractionRunRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IExtractionService extractionService,
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

        var (run, error) = await extractionService.CreateExtractionRunAsync(
            tenant.TenantId,
            workspacePublicId,
            documentPublicId,
            userId.Value,
            request,
            cancellationToken);

        if (run is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Extraction denied",
                detail: error,
                statusCode: StatusCodes.Status403Forbidden);
        }

        return Results.Created(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/extraction-runs/{run!.PublicId}",
            run);
    }

    private static async Task<IResult> GetExtractionRunAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid extractionRunPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IExtractionService extractionService,
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

        var run = await extractionService.GetExtractionRunAsync(
            tenant.TenantId,
            workspacePublicId,
            extractionRunPublicId,
            cancellationToken);

        return run is null ? Results.NotFound() : Results.Ok(run);
    }

    private static async Task<IResult> ReviewFactAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid extractionRunPublicId,
        Guid factPublicId,
        ReviewExtractedFactRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IExtractionService extractionService,
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

        var (fact, error) = await extractionService.ReviewFactAsync(
            tenant.TenantId,
            workspacePublicId,
            extractionRunPublicId,
            factPublicId,
            userId.Value,
            request,
            cancellationToken);

        if (fact is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Review denied",
                detail: error,
                statusCode: StatusCodes.Status403Forbidden);
        }

        return Results.Ok(fact);
    }

    private static async Task<IResult> ApplyAcceptedFactsAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid extractionRunPublicId,
        Guid snapshotPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IExtractionService extractionService,
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

        var (result, error) = await extractionService.ApplyAcceptedFactsAsync(
            tenant.TenantId,
            workspacePublicId,
            extractionRunPublicId,
            snapshotPublicId,
            userId.Value,
            cancellationToken);

        if (result is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Apply denied",
                detail: error,
                statusCode: StatusCodes.Status403Forbidden);
        }

        return Results.Ok(result);
    }
}
