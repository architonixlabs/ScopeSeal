using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants").WithTags("Tenants");

        group.MapGet("/{tenantPublicId:guid}", GetTenantAsync)
            .WithName("GetTenant")
            .RequireAuthorization(ScopeSealPolicies.TenantMember)
            .WithSummary("Returns tenant details when the caller is a member.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetTenantAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var tenant = await tenantService.GetTenantForUserAsync(tenantPublicId, userId, cancellationToken);
        if (tenant is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            tenant.PublicId,
            tenant.Name,
            tenant.Role,
            tenant.CreatedAtUtc
        });
    }
}
