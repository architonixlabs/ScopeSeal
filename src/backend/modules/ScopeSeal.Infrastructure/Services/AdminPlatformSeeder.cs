using Microsoft.EntityFrameworkCore;
using ScopeSeal.Administration.Domain;
using ScopeSeal.Infrastructure.Persistence;

namespace ScopeSeal.Infrastructure.Services;

public sealed class AdminPlatformSeeder(ApplicationDbContext dbContext)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await dbContext.PlatformFeatureFlags.AnyAsync(cancellationToken))
        {
            dbContext.PlatformFeatureFlags.AddRange(
                new PlatformFeatureFlag
                {
                    Id = Guid.NewGuid(),
                    Key = "AiExtractionEnabled",
                    IsEnabled = false,
                    Description = "Global kill switch for AI extraction jobs.",
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new PlatformFeatureFlag
                {
                    Id = Guid.NewGuid(),
                    Key = "RazorpayCheckoutEnabled",
                    IsEnabled = true,
                    Description = "Allows web checkout session creation.",
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new PlatformFeatureFlag
                {
                    Id = Guid.NewGuid(),
                    Key = "MaintenanceMode",
                    IsEnabled = false,
                    Description = "Blocks non-admin product writes during maintenance.",
                    UpdatedAtUtc = DateTime.UtcNow
                });
        }

        if (!await dbContext.TermsNoticeVersions.AnyAsync(cancellationToken))
        {
            dbContext.TermsNoticeVersions.Add(new TermsNoticeVersion
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                Version = "1.0",
                Title = "ScopeSeal Terms of Service (Draft)",
                Summary = "Draft terms summary for operator review. Qualified legal review required before production.",
                EffectiveFromUtc = DateTime.UtcNow,
                IsCurrent = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
