using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScopeSeal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrivacyCentre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AgeDeclaredAtUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmedAge18OrAbove",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "admin_privacy_queue_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrivacyRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    QueueStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AssignedOperator = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_privacy_queue_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "consent_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NoticeVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Granted = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WithdrawnAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "data_export_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrivacyRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DownloadToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_export_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "deletion_orchestration_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrivacyRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CurrentStep = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ScheduledBackupPurgeAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deletion_orchestration_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "privacy_notice_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_privacy_notice_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "privacy_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CorrectionDetails = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    GrievanceCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_privacy_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "retention_job_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RecordsProcessed = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retention_job_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subprocessor_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DataProcessed = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContractStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DpaStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subprocessor_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_privacy_queue_items_PrivacyRequestId",
                table: "admin_privacy_queue_items",
                column: "PrivacyRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_privacy_queue_items_PublicId",
                table: "admin_privacy_queue_items",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consent_records_PublicId",
                table: "consent_records",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consent_records_TenantId_UserId_NoticeVersionId_ConsentType",
                table: "consent_records",
                columns: new[] { "TenantId", "UserId", "NoticeVersionId", "ConsentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_data_export_jobs_PublicId",
                table: "data_export_jobs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_data_export_jobs_TenantId_UserId_Status",
                table: "data_export_jobs",
                columns: new[] { "TenantId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_deletion_orchestration_jobs_PublicId",
                table: "deletion_orchestration_jobs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deletion_orchestration_jobs_TenantId_UserId_Status",
                table: "deletion_orchestration_jobs",
                columns: new[] { "TenantId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_privacy_notice_versions_PublicId",
                table: "privacy_notice_versions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_privacy_notice_versions_Version",
                table: "privacy_notice_versions",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_privacy_requests_PublicId",
                table: "privacy_requests",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_privacy_requests_TenantId_UserId_CreatedAtUtc",
                table: "privacy_requests",
                columns: new[] { "TenantId", "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_retention_job_runs_PublicId",
                table: "retention_job_runs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subprocessor_entries_DisplayOrder",
                table: "subprocessor_entries",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_subprocessor_entries_PublicId",
                table: "subprocessor_entries",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_privacy_queue_items");

            migrationBuilder.DropTable(
                name: "consent_records");

            migrationBuilder.DropTable(
                name: "data_export_jobs");

            migrationBuilder.DropTable(
                name: "deletion_orchestration_jobs");

            migrationBuilder.DropTable(
                name: "privacy_notice_versions");

            migrationBuilder.DropTable(
                name: "privacy_requests");

            migrationBuilder.DropTable(
                name: "retention_job_runs");

            migrationBuilder.DropTable(
                name: "subprocessor_entries");

            migrationBuilder.DropColumn(
                name: "AgeDeclaredAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ConfirmedAge18OrAbove",
                table: "AspNetUsers");
        }
    }
}
