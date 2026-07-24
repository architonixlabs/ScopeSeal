using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScopeSeal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgreementSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agreement_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agreement_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "assumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assumptions_agreement_snapshots_AgreementSnapshotId",
                        column: x => x.AgreementSnapshotId,
                        principalTable: "agreement_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commitments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commitments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commitments_agreement_snapshots_AgreementSnapshotId",
                        column: x => x.AgreementSnapshotId,
                        principalTable: "agreement_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deliverables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deliverables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deliverables_agreement_snapshots_AgreementSnapshotId",
                        column: x => x.AgreementSnapshotId,
                        principalTable: "agreement_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exclusions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exclusions_agreement_snapshots_AgreementSnapshotId",
                        column: x => x.AgreementSnapshotId,
                        principalTable: "agreement_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "open_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_open_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_open_questions_agreement_snapshots_AgreementSnapshotId",
                        column: x => x.AgreementSnapshotId,
                        principalTable: "agreement_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_milestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AmountMinorUnits = table.Column<long>(type: "bigint", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_milestones_agreement_snapshots_AgreementSnapshotId",
                        column: x => x.AgreementSnapshotId,
                        principalTable: "agreement_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scope_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scope_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scope_items_agreement_snapshots_AgreementSnapshotId",
                        column: x => x.AgreementSnapshotId,
                        principalTable: "agreement_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "snapshot_dependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshot_dependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_snapshot_dependencies_agreement_snapshots_AgreementSnapshot~",
                        column: x => x.AgreementSnapshotId,
                        principalTable: "agreement_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "timeline_milestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TargetDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timeline_milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_timeline_milestones_agreement_snapshots_AgreementSnapshotId",
                        column: x => x.AgreementSnapshotId,
                        principalTable: "agreement_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agreement_snapshots_PublicId",
                table: "agreement_snapshots",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agreement_snapshots_TenantId_WorkspaceId_Status",
                table: "agreement_snapshots",
                columns: new[] { "TenantId", "WorkspaceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_assumptions_AgreementSnapshotId",
                table: "assumptions",
                column: "AgreementSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_assumptions_PublicId",
                table: "assumptions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assumptions_TenantId_AgreementSnapshotId",
                table: "assumptions",
                columns: new[] { "TenantId", "AgreementSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_commitments_AgreementSnapshotId",
                table: "commitments",
                column: "AgreementSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_commitments_PublicId",
                table: "commitments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commitments_TenantId_AgreementSnapshotId",
                table: "commitments",
                columns: new[] { "TenantId", "AgreementSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_deliverables_AgreementSnapshotId",
                table: "deliverables",
                column: "AgreementSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_deliverables_PublicId",
                table: "deliverables",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deliverables_TenantId_AgreementSnapshotId",
                table: "deliverables",
                columns: new[] { "TenantId", "AgreementSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_exclusions_AgreementSnapshotId",
                table: "exclusions",
                column: "AgreementSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_exclusions_PublicId",
                table: "exclusions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exclusions_TenantId_AgreementSnapshotId",
                table: "exclusions",
                columns: new[] { "TenantId", "AgreementSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_open_questions_AgreementSnapshotId",
                table: "open_questions",
                column: "AgreementSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_open_questions_PublicId",
                table: "open_questions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_open_questions_TenantId_AgreementSnapshotId",
                table: "open_questions",
                columns: new[] { "TenantId", "AgreementSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_milestones_AgreementSnapshotId",
                table: "payment_milestones",
                column: "AgreementSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_milestones_PublicId",
                table: "payment_milestones",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_milestones_TenantId_AgreementSnapshotId",
                table: "payment_milestones",
                columns: new[] { "TenantId", "AgreementSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_scope_items_AgreementSnapshotId",
                table: "scope_items",
                column: "AgreementSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_scope_items_PublicId",
                table: "scope_items",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scope_items_TenantId_AgreementSnapshotId",
                table: "scope_items",
                columns: new[] { "TenantId", "AgreementSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_dependencies_AgreementSnapshotId",
                table: "snapshot_dependencies",
                column: "AgreementSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_dependencies_PublicId",
                table: "snapshot_dependencies",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_dependencies_TenantId_AgreementSnapshotId",
                table: "snapshot_dependencies",
                columns: new[] { "TenantId", "AgreementSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_timeline_milestones_AgreementSnapshotId",
                table: "timeline_milestones",
                column: "AgreementSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_timeline_milestones_PublicId",
                table: "timeline_milestones",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_timeline_milestones_TenantId_AgreementSnapshotId",
                table: "timeline_milestones",
                columns: new[] { "TenantId", "AgreementSnapshotId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assumptions");

            migrationBuilder.DropTable(
                name: "commitments");

            migrationBuilder.DropTable(
                name: "deliverables");

            migrationBuilder.DropTable(
                name: "exclusions");

            migrationBuilder.DropTable(
                name: "open_questions");

            migrationBuilder.DropTable(
                name: "payment_milestones");

            migrationBuilder.DropTable(
                name: "scope_items");

            migrationBuilder.DropTable(
                name: "snapshot_dependencies");

            migrationBuilder.DropTable(
                name: "timeline_milestones");

            migrationBuilder.DropTable(
                name: "agreement_snapshots");
        }
    }
}
