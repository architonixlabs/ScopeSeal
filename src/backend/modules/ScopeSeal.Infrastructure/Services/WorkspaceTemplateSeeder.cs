using Microsoft.EntityFrameworkCore;
using Npgsql;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Workspaces.Domain;

namespace ScopeSeal.Infrastructure.Services;

public sealed class WorkspaceTemplateSeeder(ApplicationDbContext dbContext)
{
    private static readonly (string Name, string Description, WorkspaceType Type, Guid PublicId)[] SystemTemplates =
    [
        ("Blank workspace", "Start with an empty workspace and add scope manually.", WorkspaceType.General, Guid.Parse("a1000001-0000-4000-8000-000000000001")),
        ("Interior design project", "Parties, deliverables, and timeline for interior design engagements.", WorkspaceType.InteriorDesign, Guid.Parse("a1000002-0000-4000-8000-000000000002")),
        ("Contracting job", "Scope, milestones, and commitments for contracting work.", WorkspaceType.Contracting, Guid.Parse("a1000003-0000-4000-8000-000000000003")),
        ("Freelance engagement", "Simple scope and payment milestones for freelance work.", WorkspaceType.Freelance, Guid.Parse("a1000004-0000-4000-8000-000000000004"))
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var template in SystemTemplates)
        {
            var existing = await dbContext.WorkspaceTemplates
                .SingleOrDefaultAsync(t => t.PublicId == template.PublicId, cancellationToken);

            if (existing is not null)
            {
                existing.Name = template.Name;
                existing.Description = template.Description;
                existing.WorkspaceType = template.Type;
                existing.IsSystem = true;
                continue;
            }

            dbContext.WorkspaceTemplates.Add(new WorkspaceTemplate
            {
                Id = Guid.NewGuid(),
                PublicId = template.PublicId,
                TenantId = null,
                Name = template.Name,
                Description = template.Description,
                WorkspaceType = template.Type,
                IsSystem = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.ChangeTracker.Clear();
        }
    }
}
