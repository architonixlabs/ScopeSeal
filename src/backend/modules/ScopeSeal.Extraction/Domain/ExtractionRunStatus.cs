namespace ScopeSeal.Extraction.Domain;

public enum ExtractionRunStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
