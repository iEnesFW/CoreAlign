using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase81PurchasingVendorBillLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at_utc",
                table: "vendor_bills",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_user_id",
                table: "vendor_bills",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "held_at_utc",
                table: "vendor_bills",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hold_reason",
                table: "vendor_bills",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_approval",
                table: "vendor_bills",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "vendor_bill_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    purchase_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uom_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    po_unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tax_rate_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_subtotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_bill_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_bill_lines_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vendor_bill_lines_vendor_bills_vendor_bill_id",
                        column: x => x.vendor_bill_id,
                        principalTable: "vendor_bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_bill_lines_product_id",
                table: "vendor_bill_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_bill_lines_purchase_order_line_id",
                table: "vendor_bill_lines",
                column: "purchase_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_bill_lines_vendor_bill_id",
                table: "vendor_bill_lines",
                column: "vendor_bill_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vendor_bill_lines");

            migrationBuilder.DropColumn(
                name: "approved_at_utc",
                table: "vendor_bills");

            migrationBuilder.DropColumn(
                name: "approved_by_user_id",
                table: "vendor_bills");

            migrationBuilder.DropColumn(
                name: "held_at_utc",
                table: "vendor_bills");

            migrationBuilder.DropColumn(
                name: "hold_reason",
                table: "vendor_bills");

            migrationBuilder.DropColumn(
                name: "requires_approval",
                table: "vendor_bills");
        }
    }
}
