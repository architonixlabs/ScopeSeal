namespace ScopeSeal.Workspaces.Services;

public sealed record ContactSummary(
    Guid PublicId,
    string DisplayName,
    string? Email,
    string? Phone,
    string? Organization,
    DateTime CreatedAtUtc);

public sealed record CreateContactRequest(
    string DisplayName,
    string? Email,
    string? Phone,
    string? Organization);

public interface IContactService
{
    Task<IReadOnlyList<ContactSummary>> ListContactsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<ContactSummary?> GetContactAsync(
        Guid tenantId,
        Guid contactPublicId,
        CancellationToken cancellationToken = default);

    Task<ContactSummary> CreateContactAsync(
        Guid tenantId,
        CreateContactRequest request,
        CancellationToken cancellationToken = default);
}
