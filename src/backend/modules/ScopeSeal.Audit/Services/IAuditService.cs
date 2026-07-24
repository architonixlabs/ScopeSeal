using ScopeSeal.Audit.Domain;

namespace ScopeSeal.Audit.Services;

public interface IAuditService
{
    Task RecordAsync(
        Guid tenantId,
        AuditEventType eventType,
        string entityType,
        Guid entityPublicId,
        Guid? actorUserId = null,
        string? summary = null,
        CancellationToken cancellationToken = default);
}
