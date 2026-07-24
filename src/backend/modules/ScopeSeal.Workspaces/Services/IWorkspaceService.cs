using ScopeSeal.Workspaces.Domain;

namespace ScopeSeal.Workspaces.Services;

public sealed record WorkspaceSummary(
    Guid PublicId,
    string Name,
    string? Description,
    WorkspaceType Type,
    WorkspaceStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int PartyCount);

public sealed record WorkspaceDetail(
    Guid PublicId,
    string Name,
    string? Description,
    WorkspaceType Type,
    WorkspaceStatus Status,
    Guid? TemplatePublicId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<WorkspacePartySummary> Parties);

public sealed record WorkspacePartySummary(
    Guid PartyPublicId,
    string DisplayName,
    WorkspacePartyRole Role,
    string? RoleLabel,
    string? Email);

public sealed record CreateWorkspaceRequest(
    string Name,
    string? Description,
    WorkspaceType Type,
    Guid? TemplatePublicId);

public sealed record UpdateWorkspaceRequest(
    string Name,
    string? Description,
    WorkspaceType Type);

public sealed record AddWorkspacePartyRequest(
    Guid PartyPublicId,
    WorkspacePartyRole Role);

public interface IWorkspaceService
{
    Task<IReadOnlyList<WorkspaceSummary>> ListWorkspacesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<WorkspaceDetail?> GetWorkspaceAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken = default);

    Task<(WorkspaceDetail? Workspace, string? Error)> CreateWorkspaceAsync(
        Guid tenantId,
        Guid userId,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default);

    Task<(WorkspaceDetail? Workspace, string? Error)> UpdateWorkspaceAsync(
        Guid tenantId,
        Guid workspacePublicId,
        UpdateWorkspaceRequest request,
        CancellationToken cancellationToken = default);

    Task<(WorkspaceDetail? Workspace, string? Error)> ArchiveWorkspaceAsync(
        Guid tenantId,
        Guid workspacePublicId,
        CancellationToken cancellationToken = default);

    Task<(WorkspacePartySummary? Party, string? Error)> AddPartyToWorkspaceAsync(
        Guid tenantId,
        Guid workspacePublicId,
        AddWorkspacePartyRequest request,
        CancellationToken cancellationToken = default);
}
