namespace ScopeSeal.Identity.Services;

public interface IEmailVerificationService
{
    Task SendVerificationEmailAsync(
        string email,
        string displayName,
        string verificationToken,
        CancellationToken cancellationToken = default);
}
