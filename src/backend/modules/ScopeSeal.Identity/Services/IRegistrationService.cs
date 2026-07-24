using Microsoft.AspNetCore.Identity;

namespace ScopeSeal.Identity.Services;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    string TenantName);

public interface IRegistrationService
{
    Task<IdentityResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
