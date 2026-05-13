using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3OrderEnrichmentAndShipments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "actual_delivery_date",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at_utc",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_user_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "billing_address_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_address_snapshot",
                table: "orders",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancel_reason",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at_utc",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "channel",
                table: "orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_notes",
                table: "orders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_snapshot",
                table: "orders",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "due_date",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate",
                table: "orders",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "header_discount_amount",
                table: "orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "header_discount_percent",
                table: "orders",
                type: "numeric(6,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "internal_notes",
                table: "orders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_total",
                table: "orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "origin_order_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_terms_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "payment_terms_net_days_snapshot",
                table: "orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "price_list_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "promised_delivery_date",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "requested_delivery_date",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rounding_adjustment",
                table: "orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "sales_rep_user_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "shipping_address_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_address_snapshot",
                table: "orders",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_cost",
                table: "orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "submitted_at_utc",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal",
                table: "orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_total",
                table: "orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "taxable_total",
                table: "orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "withholding_total",
                table: "orders",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_kit_component",
                table: "order_lines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_manual_price_override",
                table: "order_lines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_tax_inclusive",
                table: "order_lines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_amount",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_percent",
                table: "order_lines",
                type: "numeric(6,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_net_amount",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "line_notes",
                table: "order_lines",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "line_number",
                table: "order_lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "line_subtotal",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_total",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "list_price_snapshot",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_line_id",
                table: "order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_description_snapshot",
                table: "order_lines",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_allocated",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_cancelled",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_invoiced",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_returned",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity_shipped",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "order_lines",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tax_rate_id",
                table: "order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_rate_percent",
                table: "order_lines",
                type: "numeric(6,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost_snapshot",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "uom_code",
                table: "order_lines",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "uom_conversion_factor",
                table: "order_lines",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "uom_id",
                table: "order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "warehouse_id",
                table: "order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "withholding_amount",
                table: "order_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "withholding_rate_percent",
                table: "order_lines",
                type: "numeric(6,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    picked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    packed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dispatched_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    carrier_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    tracking_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tracking_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    shipping_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    received_by = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    shipping_address_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipments", x => x.id);
                    table.ForeignKey(
                        name: "fk_shipments_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shipments_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipment_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    serial_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_cost_snapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipment_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_shipment_lines_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shipment_lines_order_lines_order_line_id",
                        column: x => x.order_line_id,
                        principalTable: "order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shipment_lines_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shipment_lines_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_due_date",
                table: "orders",
                columns: new[] { "tenant_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_status",
                table: "orders",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_tenant_id_status",
                table: "order_lines",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_shipment_lines_lot_id",
                table: "shipment_lines",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipment_lines_order_line_id",
                table: "shipment_lines",
                column: "order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipment_lines_product_id",
                table: "shipment_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipment_lines_shipment_id",
                table: "shipment_lines",
                column: "shipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_order_id",
                table: "shipments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_tenant_id_customer_id",
                table: "shipments",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_shipments_tenant_id_order_id",
                table: "shipments",
                columns: new[] { "tenant_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_shipments_tenant_id_shipment_number",
                table: "shipments",
                columns: new[] { "tenant_id", "shipment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shipments_tenant_id_status",
                table: "shipments",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_shipments_warehouse_id",
                table: "shipments",
                column: "warehouse_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipment_lines");

            migrationBuilder.DropTable(
                name: "shipments");

            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id_due_date",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id_status",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_order_lines_tenant_id_status",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "actual_delivery_date",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "approved_at_utc",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "approved_by_user_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "billing_address_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "billing_address_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancel_reason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancelled_at_utc",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "channel",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "customer_notes",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "customer_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "due_date",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "exchange_rate",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "header_discount_amount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "header_discount_percent",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "internal_notes",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "line_discount_total",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "origin_order_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "payment_terms_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "payment_terms_net_days_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "price_list_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "promised_delivery_date",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "requested_delivery_date",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "rounding_adjustment",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "sales_rep_user_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_address_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_address_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_cost",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "source",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "submitted_at_utc",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "subtotal",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "tax_total",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "taxable_total",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "type",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "withholding_total",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "is_kit_component",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "is_manual_price_override",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "is_tax_inclusive",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "line_discount_amount",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "line_discount_percent",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "line_net_amount",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "line_notes",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "line_number",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "line_subtotal",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "line_total",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "list_price_snapshot",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "parent_line_id",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "product_description_snapshot",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "quantity_allocated",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "quantity_cancelled",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "quantity_invoiced",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "quantity_returned",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "quantity_shipped",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "status",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "tax_rate_id",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "tax_rate_percent",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "unit_cost_snapshot",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "uom_code",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "uom_conversion_factor",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "uom_id",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "warehouse_id",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "withholding_amount",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "withholding_rate_percent",
                table: "order_lines");
        }
    }
}
