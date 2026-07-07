using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase128GlassDefaultTaxRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE glass_enclosure_settings_store " +
                "ADD COLUMN IF NOT EXISTS default_tax_rate_percent numeric(6,3) NOT NULL DEFAULT 20;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE glass_enclosure_settings_store DROP COLUMN IF EXISTS default_tax_rate_percent;");
        }
    }
}
