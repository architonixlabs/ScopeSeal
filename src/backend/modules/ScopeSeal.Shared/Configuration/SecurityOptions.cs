using System.ComponentModel.DataAnnotations;

namespace ScopeSeal.Shared.Configuration;

public sealed class SecurityOptions
{
    [Required]
    public SecurityHeadersOptions Headers { get; init; } = new();

    [Required]
    public RateLimitOptions RateLimit { get; init; } = new();

    [Required]
    public OpenTelemetryOptions OpenTelemetry { get; init; } = new();
}

public sealed class SecurityHeadersOptions
{
    public bool Enabled { get; init; } = true;

    [Required]
    public string ContentSecurityPolicy { get; init; } =
        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";

    [Range(0, 63072000)]
    public int StrictTransportSecurityMaxAgeSeconds { get; init; } = 31_536_000;
}

public sealed class RateLimitOptions
{
    [Range(1, 1000)]
    public int AuthPermitLimit { get; init; } = 10;

    [Range(1, 3600)]
    public int AuthWindowSeconds { get; init; } = 60;

    [Range(1, 10000)]
    public int ApiPermitLimit { get; init; } = 300;

    [Range(1, 3600)]
    public int ApiWindowSeconds { get; init; } = 60;

    [Range(1, 1000)]
    public int WebhookPermitLimit { get; init; } = 120;

    [Range(1, 3600)]
    public int WebhookWindowSeconds { get; init; } = 60;
}

public sealed class OpenTelemetryOptions
{
    public bool Enabled { get; init; }

    [Required]
    public string ServiceName { get; init; } = "ScopeSeal.Api";

    public string? OtlpEndpoint { get; init; }

    public bool ExportToConsole { get; init; }
}
