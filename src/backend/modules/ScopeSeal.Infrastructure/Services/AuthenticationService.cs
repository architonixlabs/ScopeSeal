using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScopeSeal.Identity.Authorization;
using ScopeSeal.Identity.Domain;
using ScopeSeal.Identity.Services;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Infrastructure.Services;

public sealed class AuthenticationService(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IOptions<ScopeSealOptions> options) : IUserAuthenticationService
{
    public async Task<LoginResult> SignInAsync(
        LoginRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            return new LoginResult(false, false, "Invalid email or password.");
        }

        if (options.Value.Auth.RequireEmailVerification && !user.EmailConfirmed)
        {
            return new LoginResult(false, true, "Email verification is required before sign-in.");
        }

        var membership = await dbContext.TenantMembers
            .AsNoTracking()
            .Include(m => m.Tenant)
            .Where(m => m.UserId == user.Id)
            .OrderBy(m => m.Role)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
        {
            return new LoginResult(false, false, "No tenant membership found for this account.");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            var error = result.IsLockedOut
                ? "Account is temporarily locked. Try again later."
                : "Invalid email or password.";
            return new LoginResult(false, false, error);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.DisplayName),
            new(ScopeSealClaimTypes.TenantId, membership.TenantId.ToString()),
            new(ScopeSealClaimTypes.TenantPublicId, membership.Tenant.PublicId.ToString()),
            new(ScopeSealClaimTypes.TenantRole, membership.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(options.Value.Auth.CookieExpirationHours)
            });

        return new LoginResult(true, false, null);
    }

    public Task SignOutAsync(HttpContext httpContext) =>
        httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
