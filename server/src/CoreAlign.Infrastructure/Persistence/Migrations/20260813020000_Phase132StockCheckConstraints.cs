using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase132StockCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddCheckIfMissing(migrationBuilder, "ck_stock_cost_layers_quantities_nonneg", "stock_cost_layers",
                "unit_cost >= 0 AND original_quantity >= 0 AND remaining_quantity >= 0");
            AddCheckIfMissing(migrationBuilder, "ck_stock_serial_units_unit_cost_nonneg", "stock_serial_units",
                "unit_cost >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE stock_cost_layers DROP CONSTRAINT IF EXISTS ck_stock_cost_layers_quantities_nonneg;");
            migrationBuilder.Sql("ALTER TABLE stock_serial_units DROP CONSTRAINT IF EXISTS ck_stock_serial_units_unit_cost_nonneg;");
        }

        private static void AddCheckIfMissing(MigrationBuilder migrationBuilder, string name, string table, string expression)
        {
            migrationBuilder.Sql($@"DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = '{name}') THEN
        ALTER TABLE {table} ADD CONSTRAINT {name} CHECK ({expression}) NOT VALID;
    END IF;
END $$;");
        }
    }
}
