using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase33ReturnsAndCreditNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "return_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    reason_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    source_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credit_note_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refund_payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    received_at_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credit_note_issued_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refunded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    internal_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    customer_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_return_requests_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_return_requests_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "return_request_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    uom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uom_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    quantity_returned = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_cost_snapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    tax_rate_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    tax_rate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_tax_inclusive = table.Column<bool>(type: "boolean", nullable: false),
                    line_subtotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    restockable = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_request_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_return_request_lines_order_lines_order_line_id",
                        column: x => x.order_line_id,
                        principalTable: "order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_return_request_lines_return_requests_return_request_id",
                        column: x => x.return_request_id,
                        principalTable: "return_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ix_orders_tenant_id_source_quote_id Phase34'te source_quote_id sutunu eklendikten sonra
            // yaratiliyor (orijinal ordering bug fix).

            migrationBuilder.CreateIndex(
                name: "ix_return_request_lines_order_line_id",
                table: "return_request_lines",
                column: "order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_request_lines_product_id",
                table: "return_request_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_request_lines_return_request_id",
                table: "return_request_lines",
                column: "return_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_customer_id",
                table: "return_requests",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_order_id",
                table: "return_requests",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_tenant_id_customer_id",
                table: "return_requests",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_tenant_id_order_id",
                table: "return_requests",
                columns: new[] { "tenant_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_tenant_id_requested_at_utc",
                table: "return_requests",
                columns: new[] { "tenant_id", "requested_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_tenant_id_return_number",
                table: "return_requests",
                columns: new[] { "tenant_id", "return_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_tenant_id_status",
                table: "return_requests",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "return_request_lines");

            migrationBuilder.DropTable(
                name: "return_requests");

            // ix_orders_tenant_id_source_quote_id Phase34'in Down'unda dropluyor (ordering fix).
        }
    }
}
