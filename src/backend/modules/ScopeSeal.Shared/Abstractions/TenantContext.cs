namespace ScopeSeal.Shared.Abstractions;

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    public bool IsAuthenticated => UserId.HasValue;
}
