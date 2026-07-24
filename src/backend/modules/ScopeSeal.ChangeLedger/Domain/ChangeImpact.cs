namespace ScopeSeal.ChangeLedger.Domain;

public sealed class ChangeImpact
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid ChangeRequestId { get; set; }

    public ChangeImpactType ImpactType { get; set; }

    public string Description { get; set; } = string.Empty;

    public long? AmountMinorUnits { get; set; }

    public string? CurrencyCode { get; set; }

    public int? ScheduleDaysDelta { get; set; }

    public ChangeRequest? ChangeRequest { get; set; }
}
