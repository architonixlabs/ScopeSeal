namespace ScopeSeal.Entitlements.Domain;

public sealed class TenantPlanAssignment
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid PlanVersionId { get; set; }

    public PlanVersion PlanVersion { get; set; } = null!;

    public EntitlementSource Source { get; set; }

    public DateTime AssignedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }
}
