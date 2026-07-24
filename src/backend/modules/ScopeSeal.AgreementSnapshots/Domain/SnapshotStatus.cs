namespace ScopeSeal.AgreementSnapshots.Domain;

public enum SnapshotStatus
{
    Draft = 0,
    InternalReview = 1,
    Shared = 2,
    ChangesRequested = 3,
    ReadyForApproval = 4,
    Approved = 5,
    Superseded = 6,
    Withdrawn = 7,
    Archived = 8
}
