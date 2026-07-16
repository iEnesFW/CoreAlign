using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase130PricingConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE pricing_tax_rules ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE pricing_discount_rules ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE price_lists ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE price_list_items ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE pricing_tax_rules DROP COLUMN IF EXISTS concurrency_token;");
            migrationBuilder.Sql("ALTER TABLE pricing_discount_rules DROP COLUMN IF EXISTS concurrency_token;");
            migrationBuilder.Sql("ALTER TABLE price_lists DROP COLUMN IF EXISTS concurrency_token;");
            migrationBuilder.Sql("ALTER TABLE price_list_items DROP COLUMN IF EXISTS concurrency_token;");
        }
    }
}
