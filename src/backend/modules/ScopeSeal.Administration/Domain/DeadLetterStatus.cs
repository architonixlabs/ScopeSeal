namespace ScopeSeal.Administration.Domain;

public enum DeadLetterStatus
{
    Open = 0,
    Requeued = 1,
    Dismissed = 2
}
