using System.ComponentModel.DataAnnotations;

namespace ScopeSeal.Shared.Configuration;

public sealed class ScopeSealOptions
{
    public const string SectionName = "ScopeSeal";

    [Required]
    [MinLength(1)]
    public string ProductName { get; init; } = "ScopeSeal";

    [Required]
    public string DefaultTimeZone { get; init; } = "Asia/Kolkata";

    [Required]
    [RegularExpression("^[A-Z]{3}$")]
    public string DefaultCurrency { get; init; } = "INR";

    [Required]
    public AuthOptions Auth { get; init; } = new();

    [Required]
    public StorageOptions Storage { get; init; } = new();

    [Required]
    public AiOptions Ai { get; init; } = new();
}

public sealed class AuthOptions
{
    [Required]
    [MinLength(32)]
    public string JwtSecret { get; init; } = string.Empty;

    [Required]
    public string JwtIssuer { get; init; } = "scopeseal-dev";

    [Required]
    public string JwtAudience { get; init; } = "scopeseal-api";

    [Range(1, 168)]
    public int CookieExpirationHours { get; init; } = 8;

    public bool RequireEmailVerification { get; init; } = true;
}

public sealed class StorageOptions
{
    [Required]
    public string Provider { get; init; } = "Azurite";

    public string? ConnectionString { get; init; }
}

public sealed class AiOptions
{
    [Required]
    [RegularExpression("^(ManualOnly|LocalProcessing|ApprovedExternalProvider)$")]
    public string Mode { get; init; } = "ManualOnly";
}
