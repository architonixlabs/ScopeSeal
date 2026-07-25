namespace ScopeSeal.Billing.Domain;

public sealed class BillingCustomer
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string ExternalCustomerId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
