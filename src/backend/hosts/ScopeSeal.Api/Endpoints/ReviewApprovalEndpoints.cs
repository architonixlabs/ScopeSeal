using System.Security.Claims;
using ScopeSeal.Approvals.Services;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class ReviewApprovalEndpoints
{
    public static IEndpointRouteBuilder MapReviewApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/workspaces/{workspacePublicId:guid}/snapshots/{snapshotPublicId:guid}")
            .WithTags("Review and Approval");

        group.MapPost("/share", ShareSnapshotAsync)
            .WithName("ShareSnapshotForReview")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor);

        group.MapPost("/ready-for-approval", MarkReadyForApprovalAsync)
            .WithName("MarkSnapshotReadyForApproval")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor);

        group.MapPost("/invitations", CreateInvitationAsync)
            .WithName("CreateReviewInvitation")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/invitations", ListInvitationsAsync)
            .WithName("ListReviewInvitations")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapPost("/invitations/{invitationPublicId:guid}/revoke", RevokeInvitationAsync)
            .WithName("RevokeReviewInvitation")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor);

        group.MapGet("/approval", GetApprovalRecordAsync)
            .WithName("GetApprovalRecord")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        return app;
    }

    private static async Task<IResult> ShareSnapshotAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IReviewApprovalService reviewApprovalService,
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

        var (snapshot, error) = await reviewApprovalService.ShareSnapshotAsync(
            tenant.TenantId, workspacePublicId, snapshotPublicId, userId.Value, cancellationToken);

        if (snapshot is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Share denied",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Ok(snapshot);
    }

    private static async Task<IResult> MarkReadyForApprovalAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IReviewApprovalService reviewApprovalService,
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

        var (snapshot, error) = await reviewApprovalService.MarkReadyForApprovalAsync(
            tenant.TenantId, workspacePublicId, snapshotPublicId, userId.Value, cancellationToken);

        if (snapshot is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Ready for approval denied",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Ok(snapshot);
    }

    private static async Task<IResult> CreateInvitationAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        CreateReviewInvitationRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IReviewApprovalService reviewApprovalService,
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

        var (invitation, error) = await reviewApprovalService.CreateInvitationAsync(
            tenant.TenantId,
            workspacePublicId,
            snapshotPublicId,
            userId.Value,
            request,
            cancellationToken);

        if (invitation is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Invitation denied",
                detail: error,
                statusCode: StatusCodes.Status403Forbidden);
        }

        return Results.Created(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/snapshots/{snapshotPublicId}/invitations/{invitation!.PublicId}",
            invitation);
    }

    private static async Task<IResult> ListInvitationsAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IReviewApprovalService reviewApprovalService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var invitations = await reviewApprovalService.ListInvitationsAsync(
            tenant.TenantId, workspacePublicId, snapshotPublicId, cancellationToken);

        return invitations is null ? Results.NotFound() : Results.Ok(invitations);
    }

    private static async Task<IResult> RevokeInvitationAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        Guid invitationPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IReviewApprovalService reviewApprovalService,
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

        var (success, error) = await reviewApprovalService.RevokeInvitationAsync(
            tenant.TenantId,
            workspacePublicId,
            snapshotPublicId,
            invitationPublicId,
            userId.Value,
            cancellationToken);

        if (!success && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Revocation denied",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetApprovalRecordAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid snapshotPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IReviewApprovalService reviewApprovalService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var approval = await reviewApprovalService.GetApprovalRecordAsync(
            tenant.TenantId, workspacePublicId, snapshotPublicId, cancellationToken);

        return approval is null ? Results.NotFound() : Results.Ok(approval);
    }
}
