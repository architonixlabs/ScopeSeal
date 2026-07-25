using Microsoft.AspNetCore.Authorization;
using ScopeSeal.Tenancy.Domain;

namespace ScopeSeal.Identity.Authorization;

public sealed class TenantRoleRequirement(TenantRole minimumRole) : IAuthorizationRequirement
{
    public TenantRole MinimumRole { get; } = minimumRole;
}

public sealed class TenantRoleAuthorizationHandler : AuthorizationHandler<TenantRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRoleRequirement requirement)
    {
        var roleClaim = context.User.FindFirst(ScopeSealClaimTypes.TenantRole)?.Value;
        if (roleClaim is null || !Enum.TryParse<TenantRole>(roleClaim, out var role))
        {
            return Task.CompletedTask;
        }

        if (role <= requirement.MinimumRole)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
