using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase38UniversalGlassModelTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "curtain_wall_cassette_spec_json",
                table: "glass_projects",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "eave_height_mm",
                table: "glass_projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "enclosure_category",
                table: "glass_projects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Vertical");

            migrationBuilder.AddColumn<string>(
                name: "enclosure_subtype",
                table: "glass_projects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Balcony");

            migrationBuilder.AddColumn<string>(
                name: "geometry_mode",
                table: "glass_projects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Planar");

            migrationBuilder.AddColumn<string>(
                name: "metadata_json",
                table: "glass_projects",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mounting_topology",
                table: "glass_projects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ProfileFramed");

            migrationBuilder.AddColumn<string>(
                name: "polygon_vertices_json",
                table: "glass_projects",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ridge_height_mm",
                table: "glass_projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "roof_pitch_deg",
                table: "glass_projects",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "geom_arc_radius_mm",
                table: "glass_project_runs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "geom_arc_sweep_deg",
                table: "glass_project_runs",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "geom_tilt_deg",
                table: "glass_project_runs",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "geom_z",
                table: "glass_project_runs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "panel_kind",
                table: "glass_project_panels",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Rectangular");

            migrationBuilder.CreateIndex(
                name: "ix_glass_projects_tenant_id_category_subtype",
                table: "glass_projects",
                columns: new[] { "tenant_id", "enclosure_category", "enclosure_subtype" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_glass_projects_tenant_id_category_subtype",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "curtain_wall_cassette_spec_json",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "eave_height_mm",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "enclosure_category",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "enclosure_subtype",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "geometry_mode",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "metadata_json",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "mounting_topology",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "polygon_vertices_json",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "ridge_height_mm",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "roof_pitch_deg",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "geom_arc_radius_mm",
                table: "glass_project_runs");

            migrationBuilder.DropColumn(
                name: "geom_arc_sweep_deg",
                table: "glass_project_runs");

            migrationBuilder.DropColumn(
                name: "geom_tilt_deg",
                table: "glass_project_runs");

            migrationBuilder.DropColumn(
                name: "geom_z",
                table: "glass_project_runs");

            migrationBuilder.DropColumn(
                name: "panel_kind",
                table: "glass_project_panels");
        }
    }
}
