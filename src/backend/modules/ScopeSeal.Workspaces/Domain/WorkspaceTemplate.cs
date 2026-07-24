namespace ScopeSeal.Workspaces.Domain;

public sealed class WorkspaceTemplate
{
    public Guid Id { get; set; }

    public Guid PublicId { get; set; }

    public Guid? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public WorkspaceType WorkspaceType { get; set; } = WorkspaceType.General;

    public bool IsSystem { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
