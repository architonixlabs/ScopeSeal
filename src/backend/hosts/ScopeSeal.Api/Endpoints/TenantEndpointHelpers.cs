using System.Security.Claims;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Api.Endpoints;

internal static class TenantEndpointHelpers
{
    internal static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    internal static async Task<TenantSummary?> ResolveTenantAsync(
        Guid tenantPublicId,
        ClaimsPrincipal user,
        ITenantService tenantService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return null;
        }

        return await tenantService.GetTenantForUserAsync(tenantPublicId, userId.Value, cancellationToken);
    }
}
