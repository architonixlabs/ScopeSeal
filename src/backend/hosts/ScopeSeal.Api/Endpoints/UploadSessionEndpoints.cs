using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ScopeSeal.Documents.Services;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class UploadSessionEndpoints
{
    public static IEndpointRouteBuilder MapUploadSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/workspaces/{workspacePublicId:guid}/upload-sessions")
            .WithTags("Upload Sessions");

        group.MapPost("/", CreateUploadSessionAsync)
            .WithName("CreateUploadSession")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/{sessionPublicId:guid}/content", UploadContentAsync)
            .WithName("UploadSessionContent")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .DisableAntiforgery()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/{sessionPublicId:guid}/complete", CompleteUploadSessionAsync)
            .WithName("CompleteUploadSession")
            .RequireAuthorization(ScopeSealPolicies.TenantEditor)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/{sessionPublicId:guid}", GetUploadSessionAsync)
            .WithName("GetUploadSession")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        return app;
    }

    private static async Task<IResult> CreateUploadSessionAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        CreateUploadSessionRequestBody body,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IUploadSessionService uploadSessionService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var userId = TenantEndpointHelpers.GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var (session, error) = await uploadSessionService.CreateSessionAsync(
            tenant.TenantId,
            userId.Value,
            new CreateUploadSessionRequest(
                workspacePublicId,
                body.OriginalFileName,
                body.DeclaredContentType,
                body.ExpectedBytes),
            cancellationToken);

        if (session is null)
        {
            var status = error?.Contains("limit", StringComparison.OrdinalIgnoreCase) == true ||
                         error?.Contains("not allowed", StringComparison.OrdinalIgnoreCase) == true ||
                         error?.Contains("maximum upload size", StringComparison.OrdinalIgnoreCase) == true
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;

            return Results.Problem(
                title: "Upload session could not be created.",
                detail: error,
                statusCode: status);
        }

        return Results.Created(
            $"/api/v1/tenants/{tenantPublicId}/workspaces/{workspacePublicId}/upload-sessions/{session.PublicId}",
            session);
    }

    private static async Task<IResult> UploadContentAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid sessionPublicId,
        HttpRequest request,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IUploadSessionService uploadSessionService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        if (!request.HasFormContentType || request.Form.Files.Count == 0)
        {
            return Results.Problem(
                title: "Invalid upload.",
                detail: "Multipart form file content is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var file = request.Form.Files[0];
        await using var stream = file.OpenReadStream();
        var (session, error) = await uploadSessionService.UploadContentAsync(
            tenant.TenantId,
            workspacePublicId,
            sessionPublicId,
            stream,
            file.Length,
            cancellationToken);

        if (session is null)
        {
            return Results.Problem(
                title: "Upload failed.",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(session);
    }

    private static async Task<IResult> CompleteUploadSessionAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid sessionPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IUploadSessionService uploadSessionService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var userId = TenantEndpointHelpers.GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var (result, error) = await uploadSessionService.CompleteSessionAsync(
            tenant.TenantId,
            workspacePublicId,
            sessionPublicId,
            userId.Value,
            cancellationToken);

        if (result is null)
        {
            return Results.Problem(
                title: "Upload completion failed.",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetUploadSessionAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid sessionPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IUploadSessionService uploadSessionService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var session = await uploadSessionService.GetSessionAsync(
            tenant.TenantId,
            workspacePublicId,
            sessionPublicId,
            cancellationToken);

        return session is null ? Results.NotFound() : Results.Ok(session);
    }

    private sealed record CreateUploadSessionRequestBody(
        string OriginalFileName,
        string DeclaredContentType,
        long ExpectedBytes);
}
