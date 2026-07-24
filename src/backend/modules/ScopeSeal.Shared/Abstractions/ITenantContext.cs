namespace ScopeSeal.Shared.Abstractions;

public interface ITenantContext
{
    Guid? TenantId { get; }

    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}
