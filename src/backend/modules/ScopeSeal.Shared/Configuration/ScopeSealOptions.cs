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

    [Required]
    public DocumentUploadOptions DocumentUpload { get; init; } = new();
}

public sealed class DocumentUploadOptions
{
    [Range(1024, long.MaxValue)]
    public long MaxFileBytes { get; init; } = 25_000_000;

    [Range(1, 168)]
    public int SessionExpirationHours { get; init; } = 24;

    [Range(1, 60)]
    public int DownloadTokenExpirationMinutes { get; init; } = 5;

    [Required]
    public string QuarantineContainer { get; init; } = "quarantine";

    [Required]
    public string PermanentContainer { get; init; } = "documents";
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

    public bool KillSwitchEnabled { get; init; }

    [Range(1, 1000)]
    public int MaxFactsPerRun { get; init; } = 100;

    [Range(1, 100)]
    public int MaxExtractionJobsPerBatch { get; init; } = 5;
}
