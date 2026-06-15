using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase36CustomerPortalAndInventoryAndVendor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "applied_amount",
                table: "vendor_payments",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_voided",
                table: "vendor_payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "void_reason",
                table: "vendor_payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "voided_at_utc",
                table: "vendor_payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_glass_project_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_service",
                table: "order_lines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "source_bom_line_id",
                table: "order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_project_id",
                table: "order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payment_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    intent_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    redirect_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    provider_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    initiated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    in_app_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_notification_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_counts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    count_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    warehouse_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    planned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    counting_started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reconciled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    planned_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_counts", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_counts_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_payment_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    applied_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_payment_applications", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_payment_applications_vendor_bills_vendor_bill_id",
                        column: x => x.vendor_bill_id,
                        principalTable: "vendor_bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vendor_payment_applications_vendor_payments_vendor_payment_~",
                        column: x => x.vendor_payment_id,
                        principalTable: "vendor_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_count_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_count_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    bin_location = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    expected_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    counted_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    variance_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    snapshot_unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    variance_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    counted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    counted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_count_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_count_lines_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_count_lines_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_count_lines_stock_counts_stock_count_id",
                        column: x => x.stock_count_id,
                        principalTable: "stock_counts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_source_glass_project_id",
                table: "orders",
                columns: new[] { "tenant_id", "source_glass_project_id" },
                filter: "source_glass_project_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_source_bom_line_id",
                table: "order_lines",
                column: "source_bom_line_id",
                filter: "source_bom_line_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_source_project_id",
                table: "order_lines",
                column: "source_project_id",
                filter: "source_project_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_sessions_gateway_name_intent_id",
                table: "payment_sessions",
                columns: new[] { "gateway_name", "intent_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_sessions_tenant_id_customer_id_status",
                table: "payment_sessions",
                columns: new[] { "tenant_id", "customer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_sessions_tenant_id_invoice_id",
                table: "payment_sessions",
                columns: new[] { "tenant_id", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_notification_preferences_tenant_id_user_id_notificatio~",
                table: "user_notification_preferences",
                columns: new[] { "tenant_id", "user_id", "notification_kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_counts_tenant_id_count_number",
                table: "stock_counts",
                columns: new[] { "tenant_id", "count_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_counts_tenant_id_planned_at_utc",
                table: "stock_counts",
                columns: new[] { "tenant_id", "planned_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_stock_counts_tenant_id_warehouse_id_status",
                table: "stock_counts",
                columns: new[] { "tenant_id", "warehouse_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_counts_warehouse_id",
                table: "stock_counts",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_count_lines_lot_id",
                table: "stock_count_lines",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_count_lines_product_id",
                table: "stock_count_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_count_lines_stock_count_id",
                table: "stock_count_lines",
                column: "stock_count_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payment_applications_tenant_id_vendor_bill_id",
                table: "vendor_payment_applications",
                columns: new[] { "tenant_id", "vendor_bill_id" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payment_applications_tenant_id_vendor_payment_id",
                table: "vendor_payment_applications",
                columns: new[] { "tenant_id", "vendor_payment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payment_applications_vendor_bill_id",
                table: "vendor_payment_applications",
                column: "vendor_bill_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payment_applications_vendor_payment_id",
                table: "vendor_payment_applications",
                column: "vendor_payment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_sessions");

            migrationBuilder.DropTable(
                name: "user_notification_preferences");

            migrationBuilder.DropTable(
                name: "stock_count_lines");

            migrationBuilder.DropTable(
                name: "vendor_payment_applications");

            migrationBuilder.DropTable(
                name: "stock_counts");

            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id_source_glass_project_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_order_lines_source_bom_line_id",
                table: "order_lines");

            migrationBuilder.DropIndex(
                name: "ix_order_lines_source_project_id",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "applied_amount",
                table: "vendor_payments");

            migrationBuilder.DropColumn(
                name: "is_voided",
                table: "vendor_payments");

            migrationBuilder.DropColumn(
                name: "void_reason",
                table: "vendor_payments");

            migrationBuilder.DropColumn(
                name: "voided_at_utc",
                table: "vendor_payments");

            migrationBuilder.DropColumn(
                name: "source_glass_project_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "is_service",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "source_bom_line_id",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "source_project_id",
                table: "order_lines");
        }
    }
}
