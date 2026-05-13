using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2InventoryRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    manufacture_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    supplier_lot_ref = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    country_of_origin = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    block_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lots", x => x.id);
                    table.ForeignKey(
                        name: "fk_lots_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_reason_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    affects_cost = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_reason_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_consumed = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    allocated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    released_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_allocations", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_allocations_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_allocations_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_allocations_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bin_location = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    on_hand = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    reserved = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    avg_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    last_movement_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_items_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_items_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    serial_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    on_hand_after = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    avg_cost_after = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_document_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_reference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reason_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movements_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_stock_reason_codes_reason_code_id",
                        column: x => x.reason_code_id,
                        principalTable: "stock_reason_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_warehouses_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lots_product_id",
                table: "lots",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_lots_tenant_id_expiry_date",
                table: "lots",
                columns: new[] { "tenant_id", "expiry_date" });

            migrationBuilder.CreateIndex(
                name: "ix_lots_tenant_id_product_id_lot_number",
                table: "lots",
                columns: new[] { "tenant_id", "product_id", "lot_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_allocations_lot_id",
                table: "stock_allocations",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_allocations_product_id",
                table: "stock_allocations",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_allocations_tenant_id_order_id",
                table: "stock_allocations",
                columns: new[] { "tenant_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_allocations_tenant_id_order_line_id",
                table: "stock_allocations",
                columns: new[] { "tenant_id", "order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_allocations_tenant_id_product_id_status",
                table: "stock_allocations",
                columns: new[] { "tenant_id", "product_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_allocations_warehouse_id",
                table: "stock_allocations",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_lot_id",
                table: "stock_items",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_product_id",
                table: "stock_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_tenant_id_product_id",
                table: "stock_items",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_tenant_id_warehouse_id",
                table: "stock_items",
                columns: new[] { "tenant_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_tenant_product_warehouse_lot_unique",
                table: "stock_items",
                columns: new[] { "tenant_id", "product_id", "warehouse_id", "lot_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_items_warehouse_id",
                table: "stock_items",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_lot_id",
                table: "stock_movements",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_product_id",
                table: "stock_movements",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_reason_code_id",
                table: "stock_movements",
                column: "reason_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_tenant_id_occurred_at_utc",
                table: "stock_movements",
                columns: new[] { "tenant_id", "occurred_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_tenant_id_product_id_occurred_at_utc",
                table: "stock_movements",
                columns: new[] { "tenant_id", "product_id", "occurred_at_utc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_tenant_id_source_document_type_source_docum~",
                table: "stock_movements",
                columns: new[] { "tenant_id", "source_document_type", "source_document_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_tenant_id_warehouse_id_occurred_at_utc",
                table: "stock_movements",
                columns: new[] { "tenant_id", "warehouse_id", "occurred_at_utc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_warehouse_id",
                table: "stock_movements",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_reason_codes_tenant_id_category",
                table: "stock_reason_codes",
                columns: new[] { "tenant_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_reason_codes_tenant_id_code",
                table: "stock_reason_codes",
                columns: new[] { "tenant_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_allocations");

            migrationBuilder.DropTable(
                name: "stock_items");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "lots");

            migrationBuilder.DropTable(
                name: "stock_reason_codes");
        }
    }
}
