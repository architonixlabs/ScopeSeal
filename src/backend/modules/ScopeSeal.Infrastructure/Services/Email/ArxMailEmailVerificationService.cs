using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScopeSeal.Identity.Services;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Infrastructure.Services.Email;

public sealed class ArxMailEmailVerificationService(
    ArxMailGatewayClient gateway,
    IOptions<ScopeSealOptions> options,
    ILogger<ArxMailEmailVerificationService> logger)
    : IEmailVerificationService
{
    public async Task SendVerificationEmailAsync(
        string email,
        string displayName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var productName = options.Value.ProductName;
        var message =
            $"Hello {displayName},\n\n" +
            $"Use this verification token to confirm your {productName} account email address:\n\n" +
            $"{verificationToken}\n\n" +
            "If you did not create this account, you can ignore this message.";

        var accepted = await gateway.SubmitAsync(
            new ArxMailSubmission
            {
                Name = displayName,
                Email = email,
                Subject = $"{productName} — verify your email address",
                Message = message
            },
            cancellationToken);

        if (!accepted)
        {
            logger.LogWarning(
                "Email verification message for {Email} was not accepted by ArxMail.",
                email);
        }
    }
}
