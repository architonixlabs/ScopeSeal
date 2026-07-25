using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScopeSeal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChangeRequestId",
                table: "agreement_snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceSnapshotId",
                table: "agreement_snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "change_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResultSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProposedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImplementedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_change_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "change_decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PreviousStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_change_decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_change_decisions_change_requests_ChangeRequestId",
                        column: x => x.ChangeRequestId,
                        principalTable: "change_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "change_impacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImpactType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AmountMinorUnits = table.Column<long>(type: "bigint", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ScheduleDaysDelta = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_change_impacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_change_impacts_change_requests_ChangeRequestId",
                        column: x => x.ChangeRequestId,
                        principalTable: "change_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agreement_snapshots_ChangeRequestId",
                table: "agreement_snapshots",
                column: "ChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_agreement_snapshots_SourceSnapshotId",
                table: "agreement_snapshots",
                column: "SourceSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_change_decisions_ChangeRequestId",
                table: "change_decisions",
                column: "ChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_change_decisions_PublicId",
                table: "change_decisions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_change_decisions_TenantId_ChangeRequestId",
                table: "change_decisions",
                columns: new[] { "TenantId", "ChangeRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_change_impacts_ChangeRequestId",
                table: "change_impacts",
                column: "ChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_change_impacts_PublicId",
                table: "change_impacts",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_change_impacts_TenantId_ChangeRequestId",
                table: "change_impacts",
                columns: new[] { "TenantId", "ChangeRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_change_requests_PublicId",
                table: "change_requests",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_change_requests_ResultSnapshotId",
                table: "change_requests",
                column: "ResultSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_change_requests_SourceSnapshotId",
                table: "change_requests",
                column: "SourceSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_change_requests_TenantId_WorkspaceId_Status",
                table: "change_requests",
                columns: new[] { "TenantId", "WorkspaceId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "change_decisions");

            migrationBuilder.DropTable(
                name: "change_impacts");

            migrationBuilder.DropTable(
                name: "change_requests");

            migrationBuilder.DropIndex(
                name: "IX_agreement_snapshots_ChangeRequestId",
                table: "agreement_snapshots");

            migrationBuilder.DropIndex(
                name: "IX_agreement_snapshots_SourceSnapshotId",
                table: "agreement_snapshots");

            migrationBuilder.DropColumn(
                name: "ChangeRequestId",
                table: "agreement_snapshots");

            migrationBuilder.DropColumn(
                name: "SourceSnapshotId",
                table: "agreement_snapshots");
        }
    }
}
