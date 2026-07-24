using ScopeSeal.Workspaces.Domain;

namespace ScopeSeal.Workspaces.Services;

public sealed record DashboardSummary(
    int TotalWorkspaces,
    int ActiveWorkspaces,
    int DraftWorkspaces,
    int ArchivedWorkspaces,
    int TotalContacts,
    int TotalParties,
    long ActiveWorkspaceLimit,
    long ActiveWorkspaceUsage,
    IReadOnlyList<WorkspaceSummary> RecentWorkspaces);

public sealed record WorkspaceTemplateSummary(
    Guid PublicId,
    string Name,
    string? Description,
    WorkspaceType WorkspaceType,
    bool IsSystem);

public interface IDashboardService
{
    Task<DashboardSummary?> GetDashboardAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public interface IWorkspaceTemplateService
{
    Task<IReadOnlyList<WorkspaceTemplateSummary>> ListTemplatesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
