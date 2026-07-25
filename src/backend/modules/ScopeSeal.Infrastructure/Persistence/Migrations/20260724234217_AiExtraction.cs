using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScopeSeal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AiExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "extraction_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessingJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AiMode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_extraction_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "extracted_facts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtractionRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AmountMinorUnits = table.Column<long>(type: "bigint", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    ReviewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceDocumentName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    SourceHashValue = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourcePageNumber = table.Column<int>(type: "integer", nullable: true),
                    SourceExcerpt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_extracted_facts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_extracted_facts_extraction_runs_ExtractionRunId",
                        column: x => x.ExtractionRunId,
                        principalTable: "extraction_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_extracted_facts_ExtractionRunId",
                table: "extracted_facts",
                column: "ExtractionRunId");

            migrationBuilder.CreateIndex(
                name: "IX_extracted_facts_PublicId",
                table: "extracted_facts",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_extracted_facts_TenantId_ExtractionRunId_ReviewStatus",
                table: "extracted_facts",
                columns: new[] { "TenantId", "ExtractionRunId", "ReviewStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_extraction_runs_ProcessingJobId",
                table: "extraction_runs",
                column: "ProcessingJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_extraction_runs_PublicId",
                table: "extraction_runs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_extraction_runs_TenantId_WorkspaceId_Status",
                table: "extraction_runs",
                columns: new[] { "TenantId", "WorkspaceId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "extracted_facts");

            migrationBuilder.DropTable(
                name: "extraction_runs");
        }
    }
}
