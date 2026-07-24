using Microsoft.Extensions.Logging;

namespace ScopeSeal.Identity.Services;

public sealed class DevelopmentEmailVerificationService(ILogger<DevelopmentEmailVerificationService> logger)
    : IEmailVerificationService
{
    public Task SendVerificationEmailAsync(
        string email,
        string displayName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Development email verification for {Email} ({DisplayName}). Token: {Token}",
            email,
            displayName,
            verificationToken);

        return Task.CompletedTask;
    }
}
