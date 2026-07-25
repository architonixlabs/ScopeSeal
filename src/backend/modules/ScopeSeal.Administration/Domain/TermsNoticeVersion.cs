namespace ScopeSeal.Administration.Domain;

public sealed class TermsNoticeVersion
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public string Version { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public DateTime EffectiveFromUtc { get; set; }

    public bool IsCurrent { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
