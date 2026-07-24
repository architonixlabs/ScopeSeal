namespace ScopeSeal.Documents.Domain;

public enum UploadSessionStatus
{
    Pending = 0,
    Uploading = 1,
    Quarantined = 2,
    Scanning = 3,
    Completed = 4,
    Rejected = 5,
    Expired = 6
}
