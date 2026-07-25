using System.Security.Claims;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

public static class EntitlementEndpoints
{
    public static IEndpointRouteBuilder MapEntitlementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants").WithTags("Entitlements");

        group.MapGet("/{tenantPublicId:guid}/entitlements", GetEntitlementsAsync)
            .WithName("GetTenantEntitlements")
            .RequireAuthorization(ScopeSealPolicies.TenantMember)
            .WithSummary("Returns plan, capabilities, and usage for the tenant.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetEntitlementsAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        IEntitlementService entitlementService,
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

        var summary = await entitlementService.GetSummaryAsync(tenant.TenantId, cancellationToken);
        if (summary is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new
        {
            plan = summary.PlanCode.ToString(),
            planVersion = summary.PlanVersion,
            source = summary.Source.ToString(),
            capabilities = summary.Capabilities.Select(c => c.ToString()),
            usage = summary.Usage.ToDictionary(
                pair => pair.Key.ToString(),
                pair => new
                {
                    current = pair.Value.Current,
                    limit = pair.Value.Limit
                })
        });
    }
}
