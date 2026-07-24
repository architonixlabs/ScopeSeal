namespace ScopeSeal.Workspaces.Services;

public sealed record PartySummary(
    Guid PublicId,
    string DisplayName,
    string? RoleLabel,
    Guid? ContactPublicId,
    DateTime CreatedAtUtc);

public sealed record CreatePartyRequest(
    string DisplayName,
    string? RoleLabel,
    Guid? ContactPublicId);

public interface IPartyService
{
    Task<IReadOnlyList<PartySummary>> ListPartiesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<PartySummary?> GetPartyAsync(
        Guid tenantId,
        Guid partyPublicId,
        CancellationToken cancellationToken = default);

    Task<(PartySummary? Party, string? Error)> CreatePartyAsync(
        Guid tenantId,
        CreatePartyRequest request,
        CancellationToken cancellationToken = default);
}
