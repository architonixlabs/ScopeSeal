using System.Security.Claims;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;
using ScopeSeal.Workspaces.Services;

namespace ScopeSeal.Api.Endpoints;

public static class WorkspaceTemplateEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/templates")
            .WithTags("Workspace Templates");

        group.MapGet("/", ListTemplatesAsync)
            .WithName("ListWorkspaceTemplates")
            .RequireAuthorization(ScopeSealPolicies.TenantMember);

        return app;
    }

    private static async Task<IResult> ListTemplatesAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IWorkspaceTemplateService templateService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var templates = await templateService.ListTemplatesAsync(tenant.TenantId, cancellationToken);
        return Results.Ok(templates);
    }
}
