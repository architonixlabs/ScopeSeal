using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;
using ScopeSeal.Workspaces.Services;

namespace ScopeSeal.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants/{tenantPublicId:guid}/dashboard")
            .WithTags("Dashboard");

        group.MapGet("/", GetDashboardAsync)
            .WithName("GetDashboard")
            .RequireAuthorization(ScopeSealPolicies.TenantMember)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetDashboardAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        var tenant = await TenantEndpointHelpers.ResolveTenantAsync(
            tenantPublicId, user, tenantService, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        var dashboard = await dashboardService.GetDashboardAsync(tenant.TenantId, cancellationToken);
        if (dashboard is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(dashboard);
    }
}
