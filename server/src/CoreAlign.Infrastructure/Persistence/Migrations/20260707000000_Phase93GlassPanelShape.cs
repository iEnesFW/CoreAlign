using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase93GlassPanelShape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "arch_rise_mm",
                table: "glass_project_panels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "corner_radius_bl_mm",
                table: "glass_project_panels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "corner_radius_br_mm",
                table: "glass_project_panels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "corner_radius_tl_mm",
                table: "glass_project_panels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "corner_radius_tr_mm",
                table: "glass_project_panels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "height_mm",
                table: "glass_project_panels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "top_right_height_mm",
                table: "glass_project_panels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "top_shape",
                table: "glass_project_panels",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "arch_rise_mm",
                table: "glass_project_panels");

            migrationBuilder.DropColumn(
                name: "corner_radius_bl_mm",
                table: "glass_project_panels");

            migrationBuilder.DropColumn(
                name: "corner_radius_br_mm",
                table: "glass_project_panels");

            migrationBuilder.DropColumn(
                name: "corner_radius_tl_mm",
                table: "glass_project_panels");

            migrationBuilder.DropColumn(
                name: "corner_radius_tr_mm",
                table: "glass_project_panels");

            migrationBuilder.DropColumn(
                name: "height_mm",
                table: "glass_project_panels");

            migrationBuilder.DropColumn(
                name: "top_right_height_mm",
                table: "glass_project_panels");

            migrationBuilder.DropColumn(
                name: "top_shape",
                table: "glass_project_panels");
        }
    }
}
