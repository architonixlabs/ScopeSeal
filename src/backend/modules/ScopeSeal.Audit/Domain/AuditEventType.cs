namespace ScopeSeal.Audit.Domain;

public enum AuditEventType
{
    WorkspaceCreated = 0,
    WorkspaceUpdated = 1,
    WorkspaceArchived = 2,
    ContactCreated = 3,
    PartyCreated = 4,
    WorkspacePartyAdded = 5,
    UploadSessionCreated = 6,
    DocumentUploaded = 7,
    UploadRejected = 8,
    SnapshotCreated = 9,
    SnapshotUpdated = 10,
    SnapshotShared = 11,
    SnapshotReadyForApproval = 12,
    SnapshotChangesRequested = 13,
    SnapshotApproved = 14,
    ReviewInvitationSent = 15,
    ReviewInvitationRevoked = 16,
    ReviewCommentAdded = 17,
    ChangeSuggestionAdded = 18,
    ChangeRequestCreated = 19,
    ChangeRequestStatusChanged = 20,
    ChangeRequestAccepted = 21,
    ChangeRequestImplemented = 22
}
