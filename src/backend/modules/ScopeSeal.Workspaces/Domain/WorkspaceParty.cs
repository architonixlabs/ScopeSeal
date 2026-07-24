namespace ScopeSeal.Workspaces.Domain;

public sealed class WorkspaceParty
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Workspace Workspace { get; set; } = null!;

    public Guid PartyId { get; set; }

    public Party Party { get; set; } = null!;

    public WorkspacePartyRole Role { get; set; } = WorkspacePartyRole.Client;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
