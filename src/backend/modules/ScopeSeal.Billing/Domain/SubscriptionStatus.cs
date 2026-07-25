namespace ScopeSeal.Billing.Domain;

public enum SubscriptionStatus
{
    Created = 0,
    Authenticated = 1,
    Active = 2,
    Pending = 3,
    Halted = 4,
    Paused = 5,
    Cancelled = 6,
    Completed = 7,
    GracePeriod = 8
}
