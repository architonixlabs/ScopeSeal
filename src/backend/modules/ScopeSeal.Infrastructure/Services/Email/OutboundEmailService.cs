using Microsoft.Extensions.Logging;
using ScopeSeal.Infrastructure.Services.Email;

namespace ScopeSeal.Infrastructure.Services.Email;

public interface IOutboundEmailService
{
    Task<bool> SendAsync(
        string? recipientName,
        string? recipientEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default);
}

public sealed class DevelopmentOutboundEmailService(ILogger<DevelopmentOutboundEmailService> logger)
    : IOutboundEmailService
{
    public Task<bool> SendAsync(
        string? recipientName,
        string? recipientEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Development outbound email to {Email} ({Name}). Subject: {Subject}. Body: {Message}",
            recipientEmail,
            recipientName,
            subject,
            message);

        return Task.FromResult(true);
    }
}

public sealed class ArxMailOutboundEmailService(ArxMailGatewayClient gateway) : IOutboundEmailService
{
    public Task<bool> SendAsync(
        string? recipientName,
        string? recipientEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default) =>
        gateway.SubmitAsync(
            new ArxMailSubmission
            {
                Name = recipientName,
                Email = recipientEmail,
                Subject = subject,
                Message = message
            },
            cancellationToken);
}
