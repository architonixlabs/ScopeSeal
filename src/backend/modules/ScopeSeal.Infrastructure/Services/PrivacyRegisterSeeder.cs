using Microsoft.EntityFrameworkCore;
using Npgsql;
using ScopeSeal.Infrastructure.Persistence;
using ScopeSeal.Privacy.Domain;

namespace ScopeSeal.Infrastructure.Services;

public sealed class PrivacyRegisterSeeder(ApplicationDbContext dbContext)
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedLock.WaitAsync(cancellationToken);
        try
        {
            await SeedNoticeAsync(cancellationToken);
            await SeedSubprocessorsAsync(cancellationToken);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                dbContext.ChangeTracker.Clear();
            }
        }
        finally
        {
            SeedLock.Release();
        }
    }

    private async Task SeedNoticeAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.PrivacyNoticeVersions.AnyAsync(cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.PrivacyNoticeVersions.Add(new PrivacyNoticeVersion
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid(),
            Version = "1.0",
            Title = "ScopeSeal Privacy Notice (Draft)",
            Summary =
                "This draft notice describes how ScopeSeal processes account, workspace, and approval-record data. " +
                "It is provided for product development and requires qualified legal review before production use.",
            EffectiveFromUtc = DateTime.UtcNow,
            IsCurrent = true,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private async Task SeedSubprocessorsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.SubprocessorEntries.AnyAsync(cancellationToken))
        {
            return;
        }

        var entries = new[]
        {
            new SubprocessorEntry
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                Name = "Microsoft Azure",
                Purpose = "Hosting, database, and blob storage",
                DataProcessed = "Service data required for operation",
                Location = "India (target region)",
                ContractStatus = "Draft",
                DpaStatus = "Draft",
                IsActive = true,
                DisplayOrder = 1
            },
            new SubprocessorEntry
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                Name = "Razorpay",
                Purpose = "Web subscription payments",
                DataProcessed = "Billing metadata only",
                Location = "India",
                ContractStatus = "Draft",
                DpaStatus = "Draft",
                IsActive = true,
                DisplayOrder = 2
            },
            new SubprocessorEntry
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                Name = "Email provider (TBD)",
                Purpose = "Transactional email",
                DataProcessed = "Email address and display name",
                Location = "TBD",
                ContractStatus = "Not active",
                DpaStatus = "Not active",
                IsActive = true,
                DisplayOrder = 3
            }
        };

        dbContext.SubprocessorEntries.AddRange(entries);
    }
}
