namespace ScopeSeal.ChangeLedger.Domain;

public enum ChangeRequestStatus
{
    Proposed = 0,
    UnderDiscussion = 1,
    PricingRequired = 2,
    ScheduleReviewRequired = 3,
    Accepted = 4,
    Rejected = 5,
    Withdrawn = 6,
    Implemented = 7
}
