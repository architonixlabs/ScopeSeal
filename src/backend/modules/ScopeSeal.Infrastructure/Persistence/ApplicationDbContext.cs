using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Identity.Domain;
using ScopeSeal.Tenancy.Domain;

namespace ScopeSeal.Infrastructure.Persistence;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantMember> TenantMembers => Set<TenantMember>();

    public DbSet<PlanVersion> PlanVersions => Set<PlanVersion>();

    public DbSet<TenantPlanAssignment> TenantPlanAssignments => Set<TenantPlanAssignment>();

    public DbSet<UsageCounter> UsageCounters => Set<UsageCounter>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        builder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(t => t.PublicId).IsUnique();
        });

        builder.Entity<TenantMember>(entity =>
        {
            entity.ToTable("tenant_members");
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();
            entity.HasOne(m => m.Tenant)
                .WithMany(t => t.Members)
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PlanVersion>(entity =>
        {
            entity.ToTable("plan_versions");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.PlanCode).HasConversion<string>().HasMaxLength(32);
            entity.Property(v => v.LimitsJson).HasColumnType("jsonb");
            entity.HasIndex(v => new { v.PlanCode, v.Version }).IsUnique();
        });

        builder.Entity<TenantPlanAssignment>(entity =>
        {
            entity.ToTable("tenant_plan_assignments");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Source).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(a => new { a.TenantId, a.RevokedAtUtc });
            entity.HasOne(a => a.PlanVersion)
                .WithMany(v => v.Assignments)
                .HasForeignKey(a => a.PlanVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UsageCounter>(entity =>
        {
            entity.ToTable("usage_counters");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Metric).HasConversion<string>().HasMaxLength(64);
            entity.Property(c => c.PeriodKey).HasMaxLength(16);
            entity.HasIndex(c => new { c.TenantId, c.Metric, c.PeriodKey }).IsUnique();
        });

        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
    }
}
