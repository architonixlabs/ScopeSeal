using ScopeSeal.Audit.Domain;
using ScopeSeal.Audit.Services;
using ScopeSeal.Infrastructure.Persistence;

namespace ScopeSeal.Infrastructure.Services;

public sealed class AuditService(ApplicationDbContext dbContext) : IAuditService
{
    public async Task RecordAsync(
        Guid tenantId,
        AuditEventType eventType,
        string entityType,
        Guid entityPublicId,
        Guid? actorUserId = null,
        string? summary = null,
        CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType,
            EntityType = entityType,
            EntityPublicId = entityPublicId,
            ActorUserId = actorUserId,
            Summary = summary,
            OccurredAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
