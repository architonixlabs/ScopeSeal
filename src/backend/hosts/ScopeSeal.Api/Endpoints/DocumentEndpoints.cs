using System.Security.Claims;
using ScopeSeal.Documents.Services;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}")
            .WithTags("Documents");

        group.MapGet("/workspaces/{workspacePublicId:guid}/documents", ListDocumentsAsync)
            .WithName("ListDocuments")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapGet("/workspaces/{workspacePublicId:guid}/documents/{documentPublicId:guid}", GetDocumentAsync)
            .WithName("GetDocument")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapPost("/workspaces/{workspacePublicId:guid}/documents/{documentPublicId:guid}/download-token", CreateDownloadTokenAsync)
            .WithName("CreateDocumentDownloadToken")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        group.MapGet("/documents/download", DownloadDocumentAsync)
            .WithName("DownloadDocument")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        return app;
    }

    private static async Task<IResult> ListDocumentsAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var documents = await documentService.ListDocumentsAsync(
            tenant.TenantId,
            workspacePublicId,
            cancellationToken);

        return documents is null ? Results.NotFound() : Results.Ok(documents);
    }

    private static async Task<IResult> GetDocumentAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid documentPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var document = await documentService.GetDocumentAsync(
            tenant.TenantId,
            workspacePublicId,
            documentPublicId,
            cancellationToken);

        return document is null ? Results.NotFound() : Results.Ok(document);
    }

    private static async Task<IResult> CreateDownloadTokenAsync(
        Guid tenantPublicId,
        Guid workspacePublicId,
        Guid documentPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var (token, error) = await documentService.CreateDownloadTokenAsync(
            tenant.TenantId,
            workspacePublicId,
            documentPublicId,
            cancellationToken);

        if (token is null)
        {
            return Results.Problem(
                title: "Download token could not be created.",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new
        {
            token = token.Token,
            token.ExpiresAtUtc,
            downloadPath = $"/api/v1/tenants/{tenantPublicId}/documents/download?token={token.Token}"
        });
    }

    private static async Task<IResult> DownloadDocumentAsync(
        Guid tenantPublicId,
        Guid token,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var (download, error) = await documentService.DownloadWithTokenAsync(
            tenant.TenantId,
            token,
            cancellationToken);

        if (download is null)
        {
            return Results.Problem(
                title: "Download failed.",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.File(
            download.Content,
            download.ContentType,
            download.FileName,
            enableRangeProcessing: true);
    }
}
