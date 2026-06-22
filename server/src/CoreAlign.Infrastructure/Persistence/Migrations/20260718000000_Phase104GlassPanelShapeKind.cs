using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase104GlassPanelShapeKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE glass_project_panels ADD COLUMN IF NOT EXISTS shape_kind character varying(16);");
            migrationBuilder.Sql(
                "ALTER TABLE glass_project_panels ADD COLUMN IF NOT EXISTS shape_points_json character varying(8000);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE glass_project_panels DROP COLUMN IF EXISTS shape_kind;");
            migrationBuilder.Sql(
                "ALTER TABLE glass_project_panels DROP COLUMN IF EXISTS shape_points_json;");
        }
    }
}
