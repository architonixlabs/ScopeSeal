namespace ScopeSeal.Tenancy.Domain;

public sealed class TenantMember
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Tenant Tenant { get; set; } = null!;

    public Guid UserId { get; set; }

    public TenantRole Role { get; set; }

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
