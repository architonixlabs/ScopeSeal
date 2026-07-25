namespace ScopeSeal.Billing.Domain;

public sealed class ProcessedWebhookEvent
{
    public Guid Id { get; set; }

    public string ProviderEventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string PayloadFingerprint { get; set; } = string.Empty;

    public DateTime ProcessedAtUtc { get; set; }
}
