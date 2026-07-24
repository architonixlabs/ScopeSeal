using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScopeSeal.Entitlements.Configuration;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Entitlements.Services;
using ScopeSeal.Infrastructure.Persistence;

namespace ScopeSeal.Infrastructure.Services;

public sealed class PlanCatalogSeeder(
    ApplicationDbContext dbContext,
    IOptions<PlansOptions> plansOptions)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var planCode in Enum.GetValues<PlanCode>())
        {
            var definition = plansOptions.Value.GetDefinition(planCode);
            var existing = await dbContext.PlanVersions
                .Where(v => v.PlanCode == planCode && v.Version == definition.Version)
                .SingleOrDefaultAsync(cancellationToken);

            var limitsJson = PlanLimitsSnapshot.FromDefinition(definition).ToJson();
            if (existing is not null)
            {
                if (!string.Equals(existing.LimitsJson, limitsJson, StringComparison.Ordinal))
                {
                    existing.EffectiveToUtc = DateTime.UtcNow;
                    dbContext.PlanVersions.Add(CreatePlanVersion(planCode, definition, limitsJson));
                }

                continue;
            }

            dbContext.PlanVersions.Add(CreatePlanVersion(planCode, definition, limitsJson));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PlanVersion CreatePlanVersion(
        PlanCode planCode,
        PlanDefinitionOptions definition,
        string limitsJson) => new()
    {
        Id = Guid.NewGuid(),
        PlanCode = planCode,
        Version = definition.Version,
        EffectiveFromUtc = DateTime.UtcNow,
        LimitsJson = limitsJson
    };
}
