using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScopeSeal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RazorpayBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "billing_customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processed_webhook_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_webhook_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Interval = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExternalSubscriptionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EntitlementGranted = table.Column<bool>(type: "boolean", nullable: false),
                    GracePeriodEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_billing_customers_ExternalCustomerId",
                table: "billing_customers",
                column: "ExternalCustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_billing_customers_TenantId",
                table: "billing_customers",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processed_webhook_events_PayloadFingerprint",
                table: "processed_webhook_events",
                column: "PayloadFingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processed_webhook_events_ProviderEventId",
                table: "processed_webhook_events",
                column: "ProviderEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_ExternalSubscriptionId",
                table: "tenant_subscriptions",
                column: "ExternalSubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_PublicId",
                table: "tenant_subscriptions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_TenantId_Status",
                table: "tenant_subscriptions",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_customers");

            migrationBuilder.DropTable(
                name: "processed_webhook_events");

            migrationBuilder.DropTable(
                name: "tenant_subscriptions");
        }
    }
}
