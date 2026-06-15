using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase39ProjectTemplatesAndSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name_key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_system_template = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    subtype = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    geometry_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mounting_topology = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    default_connector_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    roof_pitch_deg = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    ridge_height_mm = table.Column<int>(type: "integer", nullable: true),
                    eave_height_mm = table.Column<int>(type: "integer", nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description_key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project_template_run_presets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    label_key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    length_mm = table.Column<int>(type: "integer", nullable: false),
                    height_mm = table.Column<int>(type: "integer", nullable: false),
                    origin_x = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    origin_y = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    rotation_deg = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    default_panel_count = table.Column<int>(type: "integer", nullable: false),
                    default_panel_width_mm = table.Column<int>(type: "integer", nullable: false),
                    default_opening_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    has_top_drip = table.Column<bool>(type: "boolean", nullable: false),
                    has_bottom_threshold = table.Column<bool>(type: "boolean", nullable: false),
                    connects_to_previous_as_corner = table.Column<bool>(type: "boolean", nullable: false),
                    corner_joint_angle_deg = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    corner_uses_post = table.Column<bool>(type: "boolean", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_template_run_presets", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_template_run_presets_project_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "project_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_project_template_run_presets_template_id",
                table: "project_template_run_presets",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_template_run_presets_tenant_id_template_id_order_in~",
                table: "project_template_run_presets",
                columns: new[] { "tenant_id", "template_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "ix_project_templates_is_system_template",
                table: "project_templates",
                column: "is_system_template");

            migrationBuilder.CreateIndex(
                name: "ix_project_templates_tenant_id_category_is_active",
                table: "project_templates",
                columns: new[] { "tenant_id", "category", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_project_templates_tenant_id_code",
                table: "project_templates",
                columns: new[] { "tenant_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_template_run_presets");

            migrationBuilder.DropTable(
                name: "project_templates");
        }
    }
}
