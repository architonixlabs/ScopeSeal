using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Api.DependencyInjection;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string ApiPolicy = "api";
    public const string WebhookPolicy = "webhooks";

    public static IServiceCollection AddScopeSealRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    title = "Too many requests.",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = "Rate limit exceeded. Please retry later."
                }, cancellationToken);
            };

            options.AddPolicy(AuthPolicy, httpContext =>
            {
                var rateLimit = httpContext.RequestServices
                    .GetRequiredService<IOptions<ScopeSealOptions>>().Value.Security.RateLimit;
                return RateLimitPartition.GetFixedWindowLimiter(
                    "auth-global",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimit.AuthPermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimit.AuthWindowSeconds),
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(ApiPolicy, httpContext =>
            {
                var rateLimit = httpContext.RequestServices
                    .GetRequiredService<IOptions<ScopeSealOptions>>().Value.Security.RateLimit;
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimit.ApiPermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimit.ApiWindowSeconds),
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(WebhookPolicy, httpContext =>
            {
                var rateLimit = httpContext.RequestServices
                    .GetRequiredService<IOptions<ScopeSealOptions>>().Value.Security.RateLimit;
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"webhook-{partitionKey}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimit.WebhookPermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimit.WebhookWindowSeconds),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
