using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase131DbIntegrityFksAndTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE glass_project_panel_hardware ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_stock_cost_layers_product_id ON stock_cost_layers (product_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_stock_cost_layers_warehouse_id ON stock_cost_layers (warehouse_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_incoming_invoices_linked_vendor_bill_id ON incoming_invoices (linked_vendor_bill_id);");

            AddForeignKeyIfMissing(migrationBuilder, "fk_incoming_invoices_vendor_bills_linked_vendor_bill_id", "incoming_invoices", "linked_vendor_bill_id", "vendor_bills", "SET NULL");
            AddForeignKeyIfMissing(migrationBuilder, "fk_stock_cost_layers_products_product_id", "stock_cost_layers", "product_id", "products", "RESTRICT");
            AddForeignKeyIfMissing(migrationBuilder, "fk_stock_cost_layers_stock_items_stock_item_id", "stock_cost_layers", "stock_item_id", "stock_items", "RESTRICT");
            AddForeignKeyIfMissing(migrationBuilder, "fk_stock_cost_layers_warehouses_warehouse_id", "stock_cost_layers", "warehouse_id", "warehouses", "RESTRICT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE incoming_invoices DROP CONSTRAINT IF EXISTS fk_incoming_invoices_vendor_bills_linked_vendor_bill_id;");
            migrationBuilder.Sql("ALTER TABLE stock_cost_layers DROP CONSTRAINT IF EXISTS fk_stock_cost_layers_products_product_id;");
            migrationBuilder.Sql("ALTER TABLE stock_cost_layers DROP CONSTRAINT IF EXISTS fk_stock_cost_layers_stock_items_stock_item_id;");
            migrationBuilder.Sql("ALTER TABLE stock_cost_layers DROP CONSTRAINT IF EXISTS fk_stock_cost_layers_warehouses_warehouse_id;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_stock_cost_layers_product_id;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_stock_cost_layers_warehouse_id;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_incoming_invoices_linked_vendor_bill_id;");
            migrationBuilder.Sql("ALTER TABLE glass_project_panel_hardware DROP COLUMN IF EXISTS concurrency_token;");
        }

        private static void AddForeignKeyIfMissing(MigrationBuilder migrationBuilder, string name, string table, string column, string principalTable, string onDelete)
        {
            migrationBuilder.Sql($@"DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = '{name}') THEN
        ALTER TABLE {table} ADD CONSTRAINT {name} FOREIGN KEY ({column}) REFERENCES {principalTable} (id) ON DELETE {onDelete} NOT VALID;
    END IF;
END $$;");
        }
    }
}
