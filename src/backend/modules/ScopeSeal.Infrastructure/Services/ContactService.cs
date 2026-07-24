using Microsoft.EntityFrameworkCore;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Workspaces.Domain;
using ScopeSeal.Workspaces.Services;

namespace ScopeSeal.Infrastructure.Services;

public sealed class ContactService(ApplicationDbContext dbContext) : IContactService
{
    public async Task<IReadOnlyList<ContactSummary>> ListContactsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Contacts
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ContactSummary(
                c.PublicId,
                c.DisplayName,
                c.Email,
                c.Phone,
                c.Organization,
                c.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ContactSummary?> GetContactAsync(
        Guid tenantId,
        Guid contactPublicId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Contacts
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.PublicId == contactPublicId)
            .Select(c => new ContactSummary(
                c.PublicId,
                c.DisplayName,
                c.Email,
                c.Phone,
                c.Organization,
                c.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ContactSummary> CreateContactAsync(
        Guid tenantId,
        CreateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Organization = request.Organization?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Contacts.Add(contact);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ContactSummary(
            contact.PublicId,
            contact.DisplayName,
            contact.Email,
            contact.Phone,
            contact.Organization,
            contact.CreatedAtUtc);
    }
}
