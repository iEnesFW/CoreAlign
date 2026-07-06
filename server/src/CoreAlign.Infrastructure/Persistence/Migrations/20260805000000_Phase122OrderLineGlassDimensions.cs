using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase122OrderLineGlassDimensions : Migration
    {
        // Cut dimensions on order lines: when width/height/pieces are set the line Quantity is
        // derived as the total m² so glass is priced / costed / stocked by cut area. Nullable +
        // idempotent — normal quantity-based lines are unaffected.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE order_lines ADD COLUMN IF NOT EXISTS width_mm numeric(12,2);");
            migrationBuilder.Sql("ALTER TABLE order_lines ADD COLUMN IF NOT EXISTS height_mm numeric(12,2);");
            migrationBuilder.Sql("ALTER TABLE order_lines ADD COLUMN IF NOT EXISTS pieces numeric(12,2);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE order_lines DROP COLUMN IF EXISTS width_mm;");
            migrationBuilder.Sql("ALTER TABLE order_lines DROP COLUMN IF EXISTS height_mm;");
            migrationBuilder.Sql("ALTER TABLE order_lines DROP COLUMN IF EXISTS pieces;");
        }
    }
}
