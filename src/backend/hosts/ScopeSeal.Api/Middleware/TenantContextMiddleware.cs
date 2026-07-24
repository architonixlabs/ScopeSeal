using System.Security.Claims;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Shared.Abstractions;

namespace ScopeSeal.Api.Middleware;

public sealed class TenantContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (tenantContext is TenantContext mutable && context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                mutable.UserId = userId;
            }

            var tenantIdClaim = context.User.FindFirstValue(ScopeSealClaimTypes.TenantId);
            if (Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                mutable.TenantId = tenantId;
            }
        }

        await next(context);
    }
}
