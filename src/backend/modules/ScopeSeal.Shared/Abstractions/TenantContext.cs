namespace ScopeSeal.Shared.Abstractions;

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; init; }

    public Guid? UserId { get; init; }

    public bool IsAuthenticated => UserId.HasValue;
}
