namespace ScopeSeal.Entitlements.Domain;

public sealed class UsageCounter
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public UsageMetric Metric { get; set; }

    public string PeriodKey { get; set; } = string.Empty;

    public long Count { get; set; }
}
