using Microsoft.AspNetCore.Http;

namespace ScopeSeal.Identity.Services;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResult(bool Succeeded, bool EmailNotConfirmed, string? Error);

public interface IUserAuthenticationService
{
    Task<LoginResult> SignInAsync(
        LoginRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(HttpContext httpContext);
}
