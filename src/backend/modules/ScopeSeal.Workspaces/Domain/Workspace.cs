namespace ScopeSeal.Workspaces.Domain;

public sealed class Workspace
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public WorkspaceType Type { get; set; } = WorkspaceType.General;

    public WorkspaceStatus Status { get; set; } = WorkspaceStatus.Draft;

    public Guid? TemplateId { get; set; }

    public WorkspaceTemplate? Template { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<WorkspaceParty> Parties { get; set; } = [];
}
