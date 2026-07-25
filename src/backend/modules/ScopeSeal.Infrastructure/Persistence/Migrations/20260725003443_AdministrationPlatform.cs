using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScopeSeal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdministrationPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dead_letter_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceJobPublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequeuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dead_letter_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "platform_feature_flags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_feature_flags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "support_access_grants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    GrantedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_access_grants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "terms_notice_versions",
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
                    table.PrimaryKey("PK_terms_notice_versions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dead_letter_jobs_PublicId",
                table: "dead_letter_jobs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dead_letter_jobs_SourceJobPublicId",
                table: "dead_letter_jobs",
                column: "SourceJobPublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dead_letter_jobs_Status_FailedAtUtc",
                table: "dead_letter_jobs",
                columns: new[] { "Status", "FailedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_platform_feature_flags_Key",
                table: "platform_feature_flags",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_support_access_grants_PublicId",
                table: "support_access_grants",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_support_access_grants_TenantId_ExpiresAtUtc",
                table: "support_access_grants",
                columns: new[] { "TenantId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_terms_notice_versions_IsCurrent",
                table: "terms_notice_versions",
                column: "IsCurrent");

            migrationBuilder.CreateIndex(
                name: "IX_terms_notice_versions_PublicId",
                table: "terms_notice_versions",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letter_jobs");

            migrationBuilder.DropTable(
                name: "platform_feature_flags");

            migrationBuilder.DropTable(
                name: "support_access_grants");

            migrationBuilder.DropTable(
                name: "terms_notice_versions");
        }
    }
}
