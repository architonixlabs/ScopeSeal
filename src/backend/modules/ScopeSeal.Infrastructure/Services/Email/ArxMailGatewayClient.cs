using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Infrastructure.Services.Email;

public sealed class ArxMailGatewayClient(
    HttpClient httpClient,
    IOptions<ScopeSealOptions> options,
    ILogger<ArxMailGatewayClient> logger)
{
    public async Task<bool> SubmitAsync(
        ArxMailSubmission submission,
        CancellationToken cancellationToken = default)
    {
        var arxMail = options.Value.Notifications.Email.ArxMail;
        if (string.IsNullOrWhiteSpace(arxMail.SecretKey))
        {
            logger.LogWarning("ArxMail secret key is not configured; outbound email was skipped.");
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, arxMail.SubmitUrl);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", arxMail.SecretKey);
        request.Content = JsonContent.Create(submission);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
        {
            return true;
        }

        logger.LogWarning(
            "ArxMail gateway returned {StatusCode} for subject {Subject}",
            (int)response.StatusCode,
            submission.Subject);

        return false;
    }
}

public sealed record ArxMailSubmission
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
