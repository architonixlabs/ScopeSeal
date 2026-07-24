using System.Security.Claims;
using ScopeSeal.ChangeLedger.Services;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class ChangeLedgerEndpoints
{
    public static IEndpointRouteBuilder MapChangeLedgerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/workspaces/{workspacePublicId:guid}")
            .WithTags("Change Ledger");

        group.MapPost("/change-requests", CreateChangeRequestAsync)
            .WithName("CreateChangeRequest")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/change-requests", ListChangeRequestsAsync)
            .WithName("ListChangeRequests")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapGet("/change-requests/{changeRequestPublicId:guid}", GetChangeRequestAsync)
            .WithName("GetChangeRequest")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapPost("/change-requests/{changeRequestPublicId:guid}/transition", TransitionChangeRequestAsync)
            .WithName("TransitionChangeRequest")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/change-requests/{changeRequestPublicId:guid}/accept", AcceptChangeRequestAsync)
            .WithName("AcceptChangeRequest")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/snapshots/{fromSnapshotPublicId:guid}/diff/{toSnapshotPublicId:guid}", GetSnapshotDiffAsync)
            .WithName("GetSnapshotDiff")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        return app;
    }

    private static async Task<IResult> CreateChangeRequestAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        CreateChangeRequestRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IChangeLedgerService changeLedgerService,
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

        var (changeRequest, error) = await changeLedgerService.CreateChangeRequestAsync(
            tenant.TenantId, workspacePublicId, userId.Value, request, cancellationToken);

        if (changeRequest is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            var statusCode = error.Contains("not available", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status409Conflict;
            return Results.Problem(title: "Change request denied", detail: error, statusCode: statusCode);
        }

        return Results.Created(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/change-requests/{changeRequest!.PublicId}",
            changeRequest);
    }

    private static async Task<IResult> ListChangeRequestsAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IChangeLedgerService changeLedgerService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var changeRequests = await changeLedgerService.ListChangeRequestsAsync(
            tenant.TenantId, workspacePublicId, cancellationToken);

        return changeRequests is null ? Results.NotFound() : Results.Ok(changeRequests);
    }

    private static async Task<IResult> GetChangeRequestAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid changeRequestPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IChangeLedgerService changeLedgerService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var changeRequest = await changeLedgerService.GetChangeRequestAsync(
            tenant.TenantId, workspacePublicId, changeRequestPublicId, cancellationToken);

        return changeRequest is null ? Results.NotFound() : Results.Ok(changeRequest);
    }

    private static async Task<IResult> TransitionChangeRequestAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid changeRequestPublicId,
        TransitionChangeRequestRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IChangeLedgerService changeLedgerService,
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

        var (changeRequest, error) = await changeLedgerService.TransitionChangeRequestAsync(
            tenant.TenantId, workspacePublicId, changeRequestPublicId, userId.Value, request, cancellationToken);

        if (changeRequest is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Transition denied",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Ok(changeRequest);
    }

    private static async Task<IResult> AcceptChangeRequestAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid changeRequestPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IChangeLedgerService changeLedgerService,
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

        var (result, error) = await changeLedgerService.AcceptChangeRequestAsync(
            tenant.TenantId, workspacePublicId, changeRequestPublicId, userId.Value, cancellationToken);

        if (result is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Accept denied",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetSnapshotDiffAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid fromSnapshotPublicId,
        Guid toSnapshotPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IChangeLedgerService changeLedgerService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var diff = await changeLedgerService.GetSnapshotDiffAsync(
            tenant.TenantId, workspacePublicId, fromSnapshotPublicId, toSnapshotPublicId, cancellationToken);

        return diff is null ? Results.NotFound() : Results.Ok(diff);
    }
}
