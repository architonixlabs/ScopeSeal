using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScopeSeal.AgreementSnapshots.Domain;
using ScopeSeal.Approvals.Domain;
using ScopeSeal.Audit.Domain;
using ScopeSeal.Documents.Domain;
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

    public DbSet<UploadSession> UploadSessions => Set<UploadSession>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    public DbSet<DocumentBlob> DocumentBlobs => Set<DocumentBlob>();

    public DbSet<DocumentHash> DocumentHashes => Set<DocumentHash>();

    public DbSet<MalwareScanResult> MalwareScanResults => Set<MalwareScanResult>();

    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();

    public DbSet<DocumentDownloadToken> DocumentDownloadTokens => Set<DocumentDownloadToken>();

    public DbSet<AgreementSnapshot> AgreementSnapshots => Set<AgreementSnapshot>();

    public DbSet<ScopeItem> ScopeItems => Set<ScopeItem>();

    public DbSet<Exclusion> Exclusions => Set<Exclusion>();

    public DbSet<Deliverable> Deliverables => Set<Deliverable>();

    public DbSet<Commitment> Commitments => Set<Commitment>();

    public DbSet<PaymentMilestone> PaymentMilestones => Set<PaymentMilestone>();

    public DbSet<TimelineMilestone> TimelineMilestones => Set<TimelineMilestone>();

    public DbSet<SnapshotDependency> SnapshotDependencies => Set<SnapshotDependency>();

    public DbSet<Assumption> Assumptions => Set<Assumption>();

    public DbSet<OpenQuestion> OpenQuestions => Set<OpenQuestion>();

    public DbSet<ReviewInvitation> ReviewInvitations => Set<ReviewInvitation>();

    public DbSet<ReviewComment> ReviewComments => Set<ReviewComment>();

    public DbSet<ChangeSuggestion> ChangeSuggestions => Set<ChangeSuggestion>();

    public DbSet<ApprovalRecord> ApprovalRecords => Set<ApprovalRecord>();

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

        builder.Entity<UploadSession>(entity =>
        {
            entity.ToTable("upload_sessions");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(s => s.DeclaredContentType).HasMaxLength(128).IsRequired();
            entity.Property(s => s.ServerFileName).HasMaxLength(128).IsRequired();
            entity.Property(s => s.QuarantineBlobPath).HasMaxLength(512).IsRequired();
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(s => s.RejectionReason).HasMaxLength(500);
            entity.HasIndex(s => s.PublicId).IsUnique();
            entity.HasIndex(s => new { s.TenantId, s.WorkspaceId, s.Status });
        });

        builder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(d => d.ContentType).HasMaxLength(128).IsRequired();
            entity.Property(d => d.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(d => d.PublicId).IsUnique();
            entity.HasIndex(d => new { d.TenantId, d.WorkspaceId });
        });

        builder.Entity<DocumentVersion>(entity =>
        {
            entity.ToTable("document_versions");
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => v.PublicId).IsUnique();
            entity.HasIndex(v => new { v.DocumentId, v.VersionNumber }).IsUnique();
            entity.HasOne(v => v.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentBlob>(entity =>
        {
            entity.ToTable("document_blobs");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Container).HasMaxLength(64).IsRequired();
            entity.Property(b => b.StoragePath).HasMaxLength(512).IsRequired();
            entity.HasOne(b => b.DocumentVersion)
                .WithOne(v => v.Blob)
                .HasForeignKey<DocumentBlob>(b => b.DocumentVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentHash>(entity =>
        {
            entity.ToTable("document_hashes");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Algorithm).HasMaxLength(32).IsRequired();
            entity.Property(h => h.HashValue).HasMaxLength(128).IsRequired();
            entity.HasOne(h => h.DocumentVersion)
                .WithOne(v => v.Hash)
                .HasForeignKey<DocumentHash>(h => h.DocumentVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MalwareScanResult>(entity =>
        {
            entity.ToTable("malware_scan_results");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(r => r.ScannerName).HasMaxLength(128);
            entity.Property(r => r.Details).HasMaxLength(500);
            entity.HasOne(r => r.DocumentVersion)
                .WithOne(v => v.MalwareScan)
                .HasForeignKey<MalwareScanResult>(r => r.DocumentVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProcessingJob>(entity =>
        {
            entity.ToTable("processing_jobs");
            entity.HasKey(j => j.Id);
            entity.Property(j => j.JobType).HasConversion<string>().HasMaxLength(32);
            entity.Property(j => j.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(j => j.ErrorMessage).HasMaxLength(500);
            entity.HasIndex(j => j.PublicId).IsUnique();
            entity.HasIndex(j => new { j.TenantId, j.Status });
            entity.HasOne(j => j.DocumentVersion)
                .WithMany(v => v.ProcessingJobs)
                .HasForeignKey(j => j.DocumentVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DocumentDownloadToken>(entity =>
        {
            entity.ToTable("document_download_tokens");
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.Token).IsUnique();
            entity.HasIndex(t => new { t.TenantId, t.ExpiresAtUtc });
        });

        ConfigureSnapshotEntity(builder);
        ConfigureApprovalEntities(builder);

        builder.Entity<ScopeItem>(entity =>
        {
            entity.ToTable("scope_items");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Title).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Description).HasMaxLength(2000);
            entity.HasIndex(i => i.PublicId).IsUnique();
            entity.HasIndex(i => new { i.TenantId, i.AgreementSnapshotId });
            entity.HasOne(i => i.AgreementSnapshot)
                .WithMany(s => s.ScopeItems)
                .HasForeignKey(i => i.AgreementSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Exclusion>(entity =>
        {
            entity.ToTable("exclusions");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Title).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Description).HasMaxLength(2000);
            entity.HasIndex(i => i.PublicId).IsUnique();
            entity.HasIndex(i => new { i.TenantId, i.AgreementSnapshotId });
            entity.HasOne(i => i.AgreementSnapshot)
                .WithMany(s => s.Exclusions)
                .HasForeignKey(i => i.AgreementSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Deliverable>(entity =>
        {
            entity.ToTable("deliverables");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Title).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Description).HasMaxLength(2000);
            entity.HasIndex(i => i.PublicId).IsUnique();
            entity.HasIndex(i => new { i.TenantId, i.AgreementSnapshotId });
            entity.HasOne(i => i.AgreementSnapshot)
                .WithMany(s => s.Deliverables)
                .HasForeignKey(i => i.AgreementSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Commitment>(entity =>
        {
            entity.ToTable("commitments");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Title).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Description).HasMaxLength(2000);
            entity.HasIndex(i => i.PublicId).IsUnique();
            entity.HasIndex(i => new { i.TenantId, i.AgreementSnapshotId });
            entity.HasOne(i => i.AgreementSnapshot)
                .WithMany(s => s.Commitments)
                .HasForeignKey(i => i.AgreementSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SnapshotDependency>(entity =>
        {
            entity.ToTable("snapshot_dependencies");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Title).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Description).HasMaxLength(2000);
            entity.HasIndex(i => i.PublicId).IsUnique();
            entity.HasIndex(i => new { i.TenantId, i.AgreementSnapshotId });
            entity.HasOne(i => i.AgreementSnapshot)
                .WithMany(s => s.Dependencies)
                .HasForeignKey(i => i.AgreementSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Assumption>(entity =>
        {
            entity.ToTable("assumptions");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Title).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Description).HasMaxLength(2000);
            entity.HasIndex(i => i.PublicId).IsUnique();
            entity.HasIndex(i => new { i.TenantId, i.AgreementSnapshotId });
            entity.HasOne(i => i.AgreementSnapshot)
                .WithMany(s => s.Assumptions)
                .HasForeignKey(i => i.AgreementSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OpenQuestion>(entity =>
        {
            entity.ToTable("open_questions");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Title).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Description).HasMaxLength(2000);
            entity.HasIndex(i => i.PublicId).IsUnique();
            entity.HasIndex(i => new { i.TenantId, i.AgreementSnapshotId });
            entity.HasOne(i => i.AgreementSnapshot)
                .WithMany(s => s.OpenQuestions)
                .HasForeignKey(i => i.AgreementSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PaymentMilestone>(entity =>
        {
            entity.ToTable("payment_milestones");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Title).HasMaxLength(200).IsRequired();
            entity.Property(m => m.Description).HasMaxLength(2000);
            entity.Property(m => m.CurrencyCode).HasMaxLength(3);
            entity.HasIndex(m => m.PublicId).IsUnique();
            entity.HasIndex(m => new { m.TenantId, m.AgreementSnapshotId });
            entity.HasOne(m => m.AgreementSnapshot)
                .WithMany(s => s.PaymentMilestones)
                .HasForeignKey(m => m.AgreementSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TimelineMilestone>(entity =>
        {
            entity.ToTable("timeline_milestones");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Title).HasMaxLength(200).IsRequired();
            entity.Property(m => m.Description).HasMaxLength(2000);
            entity.HasIndex(m => m.PublicId).IsUnique();
            entity.HasIndex(m => new { m.TenantId, m.AgreementSnapshotId });
            entity.HasOne(m => m.AgreementSnapshot)
                .WithMany(s => s.TimelineMilestones)
                .HasForeignKey(m => m.AgreementSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
    }

    private static void ConfigureSnapshotEntity(ModelBuilder builder)
    {
        builder.Entity<AgreementSnapshot>(entity =>
        {
            entity.ToTable("agreement_snapshots");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Title).HasMaxLength(200).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(2000);
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(s => s.CanonicalHashSha256).HasMaxLength(64);
            entity.HasIndex(s => s.PublicId).IsUnique();
            entity.HasIndex(s => new { s.TenantId, s.WorkspaceId, s.Status });
        });
    }

    private static void ConfigureApprovalEntities(ModelBuilder builder)
    {
        builder.Entity<ReviewInvitation>(entity =>
        {
            entity.ToTable("review_invitations");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ReviewerEmail).HasMaxLength(320).IsRequired();
            entity.Property(i => i.ReviewerName).HasMaxLength(200);
            entity.Property(i => i.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(i => i.PublicId).IsUnique();
            entity.HasIndex(i => i.Token).IsUnique();
            entity.HasIndex(i => new { i.TenantId, i.AgreementSnapshotId, i.Status });
        });

        builder.Entity<ReviewComment>(entity =>
        {
            entity.ToTable("review_comments");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.AuthorName).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Content).HasMaxLength(4000).IsRequired();
            entity.HasIndex(c => c.PublicId).IsUnique();
            entity.HasIndex(c => new { c.TenantId, c.AgreementSnapshotId });
        });

        builder.Entity<ChangeSuggestion>(entity =>
        {
            entity.ToTable("change_suggestions");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.AuthorName).HasMaxLength(200).IsRequired();
            entity.Property(c => c.SectionReference).HasMaxLength(100).IsRequired();
            entity.Property(c => c.SuggestedChange).HasMaxLength(4000).IsRequired();
            entity.HasIndex(c => c.PublicId).IsUnique();
            entity.HasIndex(c => new { c.TenantId, c.AgreementSnapshotId });
        });

        builder.Entity<ApprovalRecord>(entity =>
        {
            entity.ToTable("approval_records");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.ApproverName).HasMaxLength(200).IsRequired();
            entity.Property(a => a.ApproverEmail).HasMaxLength(320).IsRequired();
            entity.Property(a => a.CanonicalHashSha256).HasMaxLength(64).IsRequired();
            entity.Property(a => a.ConfirmationStatement).HasMaxLength(1000).IsRequired();
            entity.HasIndex(a => a.PublicId).IsUnique();
            entity.HasIndex(a => new { a.TenantId, a.AgreementSnapshotId }).IsUnique();
        });
    }
}
