using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Identity.Services;
using ScopeSeal.Shared.Abstractions;
using ScopeSeal.Tenancy.Domain;

namespace ScopeSeal.Identity.DependencyInjection;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddSingleton<IdentityModule>();

        services.AddAuthorizationBuilder()
            .AddPolicy(ScopeSealPolicies.Authenticated, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(ScopeSealPolicies.TenantMember, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ScopeSealClaimTypes.TenantId);
            })
            .AddPolicy(ScopeSealPolicies.TenantAdmin, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ScopeSealClaimTypes.TenantId);
                policy.AddRequirements(new TenantRoleRequirement(TenantRole.Admin));
            })
            .AddPolicy(ScopeSealPolicies.TenantOwner, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ScopeSealClaimTypes.TenantId);
                policy.AddRequirements(new TenantRoleRequirement(TenantRole.Owner));
            });

        services.AddScoped<IAuthorizationHandler, TenantRoleAuthorizationHandler>();
        services.TryAddScoped<ITenantContext, TenantContext>();
        services.AddSingleton<IEmailVerificationService, DevelopmentEmailVerificationService>();

        return services;
    }
}

public sealed class IdentityModule : ModuleMarker;
