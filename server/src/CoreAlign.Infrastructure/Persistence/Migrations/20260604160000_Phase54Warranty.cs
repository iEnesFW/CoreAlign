using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phase 5.4 — F3.1 Warranty + Maintenance module. Adds three new tables
    /// (<c>warranty_contracts</c>, <c>maintenance_schedules</c>,
    /// <c>service_tickets</c>) consumed by the warranty service, the
    /// <c>WorkOrderInstalledWarrantyActivator</c> notification handler, and the
    /// <c>WarrantyExpiryNotifier</c> background job. All three tables are
    /// tenant-scoped; warranty contracts and service tickets are soft-deletable
    /// and concurrency-tracked.
    /// </summary>
    public partial class Phase54Warranty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "warranty_contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    coverage_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    warranty_months = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    terms_json = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warranty_contracts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_warranty_contracts_tenant_number",
                table: "warranty_contracts",
                columns: new[] { "tenant_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_warranty_contracts_tenant_customer",
                table: "warranty_contracts",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_warranty_contracts_tenant_order",
                table: "warranty_contracts",
                columns: new[] { "tenant_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_warranty_contracts_tenant_status",
                table: "warranty_contracts",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_warranty_contracts_tenant_end_date",
                table: "warranty_contracts",
                columns: new[] { "tenant_id", "end_date" });

            migrationBuilder.CreateTable(
                name: "maintenance_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warranty_contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    next_due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recurrence_pattern = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_schedules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_schedules_tenant_contract",
                table: "maintenance_schedules",
                columns: new[] { "tenant_id", "warranty_contract_id" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_schedules_tenant_next_due",
                table: "maintenance_schedules",
                columns: new[] { "tenant_id", "next_due_date" });

            migrationBuilder.CreateTable(
                name: "service_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warranty_contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description_md = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    reported_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_notes_md = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    is_under_warranty = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    chargeable_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_tickets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_tickets_tenant_customer",
                table: "service_tickets",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_service_tickets_tenant_status",
                table: "service_tickets",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_service_tickets_tenant_priority",
                table: "service_tickets",
                columns: new[] { "tenant_id", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_service_tickets_tenant_warranty",
                table: "service_tickets",
                columns: new[] { "tenant_id", "warranty_contract_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "service_tickets");
            migrationBuilder.DropTable(name: "maintenance_schedules");
            migrationBuilder.DropTable(name: "warranty_contracts");
        }
    }
}
