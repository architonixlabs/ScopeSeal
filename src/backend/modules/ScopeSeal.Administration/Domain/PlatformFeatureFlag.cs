namespace ScopeSeal.Administration.Domain;

public sealed class PlatformFeatureFlag
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
