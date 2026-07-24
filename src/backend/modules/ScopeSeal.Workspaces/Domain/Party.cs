namespace ScopeSeal.Workspaces.Domain;

public sealed class Party
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public Guid? ContactId { get; set; }

    public Contact? Contact { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? RoleLabel { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<WorkspaceParty> WorkspaceParties { get; set; } = [];
}
