using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScopeSeal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReviewAndApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "agreement_snapshots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalHashSha256",
                table: "agreement_snapshots",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "approval_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewInvitationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApproverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApproverEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CanonicalHashSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfirmationStatement = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SnapshotVersionNumber = table.Column<int>(type: "integer", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "change_suggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewInvitationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SectionReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SuggestedChange = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_change_suggestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "review_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewInvitationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "review_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ReviewerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastAccessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_invitations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_approval_records_PublicId",
                table: "approval_records",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_approval_records_TenantId_AgreementSnapshotId",
                table: "approval_records",
                columns: new[] { "TenantId", "AgreementSnapshotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_change_suggestions_PublicId",
                table: "change_suggestions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_change_suggestions_TenantId_AgreementSnapshotId",
                table: "change_suggestions",
                columns: new[] { "TenantId", "AgreementSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_review_comments_PublicId",
                table: "review_comments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_comments_TenantId_AgreementSnapshotId",
                table: "review_comments",
                columns: new[] { "TenantId", "AgreementSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_review_invitations_PublicId",
                table: "review_invitations",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_invitations_TenantId_AgreementSnapshotId_Status",
                table: "review_invitations",
                columns: new[] { "TenantId", "AgreementSnapshotId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_review_invitations_Token",
                table: "review_invitations",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_records");

            migrationBuilder.DropTable(
                name: "change_suggestions");

            migrationBuilder.DropTable(
                name: "review_comments");

            migrationBuilder.DropTable(
                name: "review_invitations");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "agreement_snapshots");

            migrationBuilder.DropColumn(
                name: "CanonicalHashSha256",
                table: "agreement_snapshots");
        }
    }
}
