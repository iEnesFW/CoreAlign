using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase123ProductCostingMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE products ADD COLUMN IF NOT EXISTS costing_method character varying(20) NOT NULL DEFAULT 'WeightedAverage';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS costing_method;");
        }
    }
}
