using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase71StockItemConcurrencyToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE stock_items ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE stock_items DROP COLUMN IF EXISTS concurrency_token;");
        }
    }
}
