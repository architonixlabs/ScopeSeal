namespace ScopeSeal.Privacy.Domain;

public enum DeletionStep
{
    AccountLock = 0,
    DataExportOffered = 1,
    ContentAnonymization = 2,
    BlobDeletionScheduled = 3,
    BackupPurgeScheduled = 4,
    Completed = 5
}
