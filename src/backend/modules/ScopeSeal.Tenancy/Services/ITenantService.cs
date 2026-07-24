using ScopeSeal.Tenancy.Domain;

namespace ScopeSeal.Tenancy.Services;

public sealed record TenantSummary(
    Guid TenantId,
    Guid PublicId,
    string Name,
    TenantRole Role,
    DateTime CreatedAtUtc);

public interface ITenantService
{
    Task<TenantSummary?> GetTenantForUserAsync(Guid tenantPublicId, Guid userId, CancellationToken cancellationToken = default);

    Task<TenantSummary?> GetCurrentTenantForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
