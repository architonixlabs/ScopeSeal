using Microsoft.EntityFrameworkCore;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Workspaces.Domain;
using ScopeSeal.Workspaces.Services;

namespace ScopeSeal.Infrastructure.Services;

public sealed class PartyService(ApplicationDbContext dbContext) : IPartyService
{
    public async Task<IReadOnlyList<PartySummary>> ListPartiesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Parties
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new PartySummary(
                p.PublicId,
                p.DisplayName,
                p.RoleLabel,
                p.Contact != null ? p.Contact.PublicId : (Guid?)null,
                p.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PartySummary?> GetPartyAsync(
        Guid tenantId,
        Guid partyPublicId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Parties
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.PublicId == partyPublicId)
            .Select(p => new PartySummary(
                p.PublicId,
                p.DisplayName,
                p.RoleLabel,
                p.Contact != null ? p.Contact.PublicId : (Guid?)null,
                p.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<(PartySummary? Party, string? Error)> CreatePartyAsync(
        Guid tenantId,
        CreatePartyRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid? contactId = null;
        if (request.ContactPublicId is not null)
        {
            var contact = await dbContext.Contacts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    c => c.TenantId == tenantId && c.PublicId == request.ContactPublicId,
                    cancellationToken);

            if (contact is null)
            {
                return (null, "Contact not found.");
            }

            contactId = contact.Id;
        }

        var party = new Party
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            TenantId = tenantId,
            ContactId = contactId,
            DisplayName = request.DisplayName.Trim(),
            RoleLabel = request.RoleLabel?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Parties.Add(party);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (new PartySummary(
            party.PublicId,
            party.DisplayName,
            party.RoleLabel,
            request.ContactPublicId,
            party.CreatedAtUtc), null);
    }
}
