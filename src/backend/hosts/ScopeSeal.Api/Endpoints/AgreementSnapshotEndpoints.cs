using System.Security.Claims;
using ScopeSeal.AgreementSnapshots.Services;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class AgreementSnapshotEndpoints
{
    public static IEndpointRouteBuilder MapAgreementSnapshotEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/workspaces/{workspacePublicId:guid}/snapshots")
            .WithTags("Agreement Snapshots");

        group.MapGet("/", ListSnapshotsAsync)
            .WithName("ListAgreementSnapshots")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapPost("/", CreateSnapshotAsync)
            .WithName("CreateAgreementSnapshot")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{snapshotPublicId:guid}", GetSnapshotAsync)
            .WithName("GetAgreementSnapshot")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapPut("/{snapshotPublicId:guid}", UpdateSnapshotAsync)
            .WithName("UpdateAgreementSnapshot")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListSnapshotsAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IAgreementSnapshotService snapshotService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var snapshots = await snapshotService.ListSnapshotsAsync(
            tenant.TenantId, workspacePublicId, cancellationToken);

        return snapshots is null ? Results.NotFound() : Results.Ok(snapshots);
    }

    private static async Task<IResult> GetSnapshotAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IAgreementSnapshotService snapshotService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var snapshot = await snapshotService.GetSnapshotAsync(
            tenant.TenantId, workspacePublicId, snapshotPublicId, cancellationToken);

        return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
    }

    private static async Task<IResult> CreateSnapshotAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        CreateAgreementSnapshotRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IAgreementSnapshotService snapshotService,
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

        var (snapshot, error) = await snapshotService.CreateSnapshotAsync(
            tenant.TenantId, workspacePublicId, userId.Value, request, cancellationToken);

        if (error is not null)
        {
            return Results.Problem(
                title: "Snapshot creation denied",
                detail: error,
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (snapshot is null)
        {
            return Results.NotFound();
        }

        return Results.Created(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshot.PublicId}",
            snapshot);
    }

    private static async Task<IResult> UpdateSnapshotAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        UpdateAgreementSnapshotRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IAgreementSnapshotService snapshotService,
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

        var (snapshot, error, isConcurrencyConflict) = await snapshotService.UpdateSnapshotAsync(
            tenant.TenantId,
            workspacePublicId,
            snapshotPublicId,
            userId.Value,
            request,
            cancellationToken);

        if (snapshot is null && error is null)
        {
            return Results.NotFound();
        }

        if (isConcurrencyConflict)
        {
            return Results.Problem(
                title: "Concurrency conflict",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Snapshot update denied",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Ok(snapshot);
    }
}
