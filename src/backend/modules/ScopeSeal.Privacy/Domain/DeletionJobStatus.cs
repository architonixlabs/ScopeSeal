namespace ScopeSeal.Privacy.Domain;

public enum DeletionJobStatus
{
    Pending = 0,
    Scheduled = 1,
    InProgress = 2,
    AwaitingBackupPurge = 3,
    Completed = 4,
    Failed = 5
}
