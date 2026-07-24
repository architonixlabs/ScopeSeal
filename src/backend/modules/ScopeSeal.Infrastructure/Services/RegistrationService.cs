using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Identity.Domain;
using ScopeSeal.Identity.Services;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Shared.Configuration;
using ScopeSeal.Tenancy.Domain;

namespace ScopeSeal.Infrastructure.Services;

public sealed class RegistrationService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IEmailVerificationService emailVerificationService,
    IEntitlementService entitlementService,
    IOptions<ScopeSealOptions> options) : IRegistrationService
{
    public async Task<IdentityResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email = request.Email.Trim().ToLowerInvariant(),
            DisplayName = request.DisplayName.Trim(),
            RequiresEmailVerification = options.Value.Auth.RequireEmailVerification,
            EmailConfirmed = !options.Value.Auth.RequireEmailVerification
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return createResult;
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            Name = request.TenantName.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var membership = new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = TenantRole.Owner,
            JoinedAtUtc = DateTime.UtcNow
        };

        dbContext.Tenants.Add(tenant);
        dbContext.TenantMembers.Add(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        await entitlementService.AssignDefaultFreePlanAsync(tenant.Id, cancellationToken);

        if (options.Value.Auth.RequireEmailVerification)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await emailVerificationService.SendVerificationEmailAsync(
                user.Email!,
                user.DisplayName,
                token,
                cancellationToken);
        }

        return IdentityResult.Success;
    }
}
