using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase126ProductConcurrencyAndGlassTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Glass Run/Panel/WorkOrder concurrency tokens are model-only facets (columns already
            // exist) — no DDL. Only products gains a new concurrency_token column.
            migrationBuilder.Sql(
                "ALTER TABLE products ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS concurrency_token;");
        }
    }
}
