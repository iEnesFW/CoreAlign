using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase56MrpLite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "preferred_supplier_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_preferred_supplier",
                table: "products",
                columns: new[] { "tenant_id", "preferred_supplier_id" });

            migrationBuilder.CreateTable(
                name: "purchase_requisitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    reason = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reject_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    converted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    converted_purchase_order_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_purchase_requisitions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_purchase_requisitions_tenant_number",
                table: "purchase_requisitions",
                columns: new[] { "tenant_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisitions_tenant_status",
                table: "purchase_requisitions",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisitions_tenant_requested_at",
                table: "purchase_requisitions",
                columns: new[] { "tenant_id", "requested_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requisition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity_requested = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    estimated_unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    preferred_supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expected_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_requisition_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_requisition_lines_purchase_requisitions_requisition_id",
                        column: x => x.requisition_id,
                        principalTable: "purchase_requisitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_purchase_requisition_lines_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisition_lines_requisition",
                table: "purchase_requisition_lines",
                column: "requisition_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisition_lines_tenant_product",
                table: "purchase_requisition_lines",
                columns: new[] { "tenant_id", "product_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "purchase_requisition_lines");
            migrationBuilder.DropTable(name: "purchase_requisitions");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_preferred_supplier",
                table: "products");

            migrationBuilder.DropColumn(
                name: "preferred_supplier_id",
                table: "products");
        }
    }
}
