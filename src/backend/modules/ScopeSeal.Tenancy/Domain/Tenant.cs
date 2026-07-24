namespace ScopeSeal.Tenancy.Domain;

public sealed class Tenant
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<TenantMember> Members { get; set; } = [];
}
