using Microsoft.EntityFrameworkCore;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Tenancy.Services;

namespace ScopeSeal.Infrastructure.Services;

public sealed class TenantService(ApplicationDbContext dbContext) : ITenantService
{
    public async Task<TenantSummary?> GetTenantForUserAsync(
        Guid tenantPublicId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.TenantMembers
            .AsNoTracking()
            .Include(m => m.Tenant)
            .Where(m => m.UserId == userId && m.Tenant.PublicId == tenantPublicId)
            .FirstOrDefaultAsync(cancellationToken);

        return membership is null
            ? null
            : new TenantSummary(
                membership.TenantId,
                membership.Tenant.PublicId,
                membership.Tenant.Name,
                membership.Role,
                membership.Tenant.CreatedAtUtc);
    }

    public async Task<TenantSummary?> GetCurrentTenantForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.TenantMembers
            .AsNoTracking()
            .Include(m => m.Tenant)
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Role)
            .FirstOrDefaultAsync(cancellationToken);

        return membership is null
            ? null
            : new TenantSummary(
                membership.TenantId,
                membership.Tenant.PublicId,
                membership.Tenant.Name,
                membership.Role,
                membership.Tenant.CreatedAtUtc);
    }
}
