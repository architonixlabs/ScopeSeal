using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;
using ScopeSeal.Workspaces.Domain;
using ScopeSeal.Workspaces.Services;

namespace ScopeSeal.Api.Endpoints;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/workspaces")
            .WithTags("Workspaces");

        group.MapGet("/", ListWorkspacesAsync)
            .WithName("ListWorkspaces")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapPost("/", CreateWorkspaceAsync)
            .WithName("CreateWorkspace")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{workspacePublicId:guid}", GetWorkspaceAsync)
            .WithName("GetWorkspace")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapPut("/{workspacePublicId:guid}", UpdateWorkspaceAsync)
            .WithName("UpdateWorkspace")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor);

        group.MapPost("/{workspacePublicId:guid}/archive", ArchiveWorkspaceAsync)
            .WithName("ArchiveWorkspace")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor);

        group.MapPost("/{workspacePublicId:guid}/parties", AddWorkspacePartyAsync)
            .WithName("AddWorkspaceParty")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor);

        return app;
    }

    private static async Task<IResult> ListWorkspacesAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var workspaces = await workspaceService.ListWorkspacesAsync(tenant.TenantId, cancellationToken);
        return Results.Ok(workspaces);
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var workspace = await workspaceService.GetWorkspaceAsync(
            tenant.TenantId, workspacePublicId, cancellationToken);

        return workspace is null ? Results.NotFound() : Results.Ok(workspace);
    }

    private static async Task<IResult> CreateWorkspaceAsync(
        Guid tenantPublicId,
        CreateWorkspaceRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IWorkspaceService workspaceService,
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

        var (workspace, error) = await workspaceService.CreateWorkspaceAsync(
            tenant.TenantId, userId.Value, request, cancellationToken);

        if (error is not null)
        {
            return Results.Problem(
                title: "Workspace creation denied",
                detail: error,
                statusCode: StatusCodes.Status403Forbidden);
        }

        return Results.Created(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspace!.PublicId}",
            workspace);
    }

    private static async Task<IResult> UpdateWorkspaceAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        UpdateWorkspaceRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var (workspace, error) = await workspaceService.UpdateWorkspaceAsync(
            tenant.TenantId, workspacePublicId, request, cancellationToken);

        if (workspace is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Workspace update denied",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Ok(workspace);
    }

    private static async Task<IResult> ArchiveWorkspaceAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var (workspace, error) = await workspaceService.ArchiveWorkspaceAsync(
            tenant.TenantId, workspacePublicId, cancellationToken);

        if (workspace is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Workspace archive denied",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Ok(workspace);
    }

    private static async Task<IResult> AddWorkspacePartyAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        AddWorkspacePartyRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IWorkspaceService workspaceService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var (party, error) = await workspaceService.AddPartyToWorkspaceAsync(
            tenant.TenantId, workspacePublicId, request, cancellationToken);

        if (party is null && error is null)
        {
            return Results.NotFound();
        }

        if (error is not null)
        {
            return Results.Problem(
                title: "Unable to add party",
                detail: error,
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Created(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/parties/{party!.PartyPublicId}",
            party);
    }
}
