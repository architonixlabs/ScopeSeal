using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Entitlements.Domain;
using ScopeSeal.Identity.Domain;
using ScopeSeal.Tenancy.Domain;
using ScopeSeal.Workspaces.Domain;

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

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<Party> Parties => Set<Party>();

    public DbSet<WorkspaceParty> WorkspaceParties => Set<WorkspaceParty>();

    public DbSet<WorkspaceTemplate> WorkspaceTemplates => Set<WorkspaceTemplate>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

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

        builder.Entity<Workspace>(entity =>
        {
            entity.ToTable("workspaces");
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Name).HasMaxLength(200).IsRequired();
            entity.Property(w => w.Description).HasMaxLength(2000);
            entity.Property(w => w.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(w => w.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(w => w.PublicId).IsUnique();
            entity.HasIndex(w => new { w.TenantId, w.Status });
            entity.HasOne(w => w.Template)
                .WithMany()
                .HasForeignKey(w => w.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Contact>(entity =>
        {
            entity.ToTable("contacts");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Email).HasMaxLength(320);
            entity.Property(c => c.Phone).HasMaxLength(32);
            entity.Property(c => c.Organization).HasMaxLength(200);
            entity.HasIndex(c => c.PublicId).IsUnique();
            entity.HasIndex(c => c.TenantId);
        });

        builder.Entity<Party>(entity =>
        {
            entity.ToTable("parties");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(p => p.RoleLabel).HasMaxLength(100);
            entity.HasIndex(p => p.PublicId).IsUnique();
            entity.HasIndex(p => p.TenantId);
            entity.HasOne(p => p.Contact)
                .WithMany(c => c.Parties)
                .HasForeignKey(p => p.ContactId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<WorkspaceParty>(entity =>
        {
            entity.ToTable("workspace_parties");
            entity.HasKey(wp => wp.Id);
            entity.Property(wp => wp.Role).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(wp => new { wp.WorkspaceId, wp.PartyId }).IsUnique();
            entity.HasOne(wp => wp.Workspace)
                .WithMany(w => w.Parties)
                .HasForeignKey(wp => wp.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(wp => wp.Party)
                .WithMany(p => p.WorkspaceParties)
                .HasForeignKey(wp => wp.PartyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkspaceTemplate>(entity =>
        {
            entity.ToTable("workspace_templates");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(1000);
            entity.Property(t => t.WorkspaceType).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(t => t.PublicId).IsUnique();
            entity.HasIndex(t => new { t.TenantId, t.IsSystem });
        });

        builder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasConversion<string>().HasMaxLength(64);
            entity.Property(e => e.EntityType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(500);
            entity.HasIndex(e => new { e.TenantId, e.OccurredAtUtc });
        });

        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
    }
}
