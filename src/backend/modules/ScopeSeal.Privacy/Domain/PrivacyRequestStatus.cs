namespace ScopeSeal.Privacy.Domain;

public enum PrivacyRequestStatus
{
    Submitted = 0,
    InReview = 1,
    Processing = 2,
    Completed = 3,
    Rejected = 4,
    Cancelled = 5
}
