using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase121ProductGlassAttributes : Migration
    {
        // First-class glass attribute columns (colour + thickness) promoted from the free-form
        // variant JSON so they can be searched / filtered / indexed. Idempotent ADD COLUMN pattern.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE products ADD COLUMN IF NOT EXISTS color character varying(60);");
            migrationBuilder.Sql("ALTER TABLE products ADD COLUMN IF NOT EXISTS thickness_mm numeric(9,2);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_products_tenant_id_color ON products (tenant_id, color);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_products_tenant_id_thickness_mm ON products (tenant_id, thickness_mm);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_products_tenant_id_color;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_products_tenant_id_thickness_mm;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS color;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS thickness_mm;");
        }
    }
}
