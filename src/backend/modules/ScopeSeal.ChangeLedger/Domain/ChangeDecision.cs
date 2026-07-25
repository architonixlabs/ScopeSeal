namespace ScopeSeal.ChangeLedger.Domain;

public sealed class ChangeDecision
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid ChangeRequestId { get; set; }

    public Guid DecidedByUserId { get; set; }

    public string? DecisionNote { get; set; }

    public ChangeRequestStatus PreviousStatus { get; set; }

    public ChangeRequestStatus NewStatus { get; set; }

    public DateTime DecidedAtUtc { get; set; }

    public ChangeRequest? ChangeRequest { get; set; }
}
