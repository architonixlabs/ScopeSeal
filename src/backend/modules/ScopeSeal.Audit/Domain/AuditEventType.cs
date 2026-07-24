namespace ScopeSeal.Audit.Domain;

public enum AuditEventType
{
    WorkspaceCreated = 0,
    WorkspaceUpdated = 1,
    WorkspaceArchived = 2,
    ContactCreated = 3,
    PartyCreated = 4,
    WorkspacePartyAdded = 5
}
