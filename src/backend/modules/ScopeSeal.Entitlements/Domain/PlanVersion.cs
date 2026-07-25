namespace ScopeSeal.Entitlements.Domain;

public sealed class PlanVersion
{
    public Guid Id { get; set; }

    public PlanCode PlanCode { get; set; }

    public int Version { get; set; }

    public DateTime EffectiveFromUtc { get; set; }

    public DateTime? EffectiveToUtc { get; set; }

    public string LimitsJson { get; set; } = string.Empty;

    public ICollection<TenantPlanAssignment> Assignments { get; set; } = [];
}
