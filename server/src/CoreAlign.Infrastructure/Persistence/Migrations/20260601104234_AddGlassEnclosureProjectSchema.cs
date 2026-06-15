using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGlassEnclosureProjectSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "glass_field_surveys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    surveyed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    surveyed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    gps_lat = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    gps_lng = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    floor_number = table.Column<int>(type: "integer", nullable: true),
                    building_height_m = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    slope_top_mm = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    slope_bottom_mm = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    slope_left_mm = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    slope_right_mm = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    raw_measurements_json = table.Column<string>(type: "jsonb", nullable: false),
                    obstacles_json = table.Column<string>(type: "jsonb", nullable: false),
                    photo_urls_json = table.Column<string>(type: "jsonb", nullable: false),
                    annotated_photo_urls_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_field_surveys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_notification_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    recipient_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    delivered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_notification_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_project_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_project_attachments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_project_bom_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ref_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_project_bom_lines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_project_change_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scene_version_from = table.Column<int>(type: "integer", nullable: false),
                    scene_version_to = table.Column<int>(type: "integer", nullable: false),
                    change_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    change_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    change_diff_json = table.Column<string>(type: "jsonb", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_project_change_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_project_cutting_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    plan_json = table.Column<string>(type: "jsonb", nullable: false),
                    total_waste_mm2 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_waste_mm = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    utilization_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    generated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    generated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_project_cutting_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_project_order_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_project_order_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_project_quote_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scene_version = table.Column<int>(type: "integer", nullable: false),
                    pdf_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    grand_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    issued_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    issued_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_project_quote_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_project_scenes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    scene_json_compressed = table.Column<byte[]>(type: "bytea", nullable: false),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    camera_state_json = table.Column<string>(type: "text", nullable: true),
                    saved_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_customer_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approval_signature_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_project_scenes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_project_share_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scene_version = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    last_viewed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    signature_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_project_share_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    site_address_line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    site_address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    site_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    site_district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    site_postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    site_country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_designer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_salesperson_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    floor_number = table.Column<int>(type: "integer", nullable: true),
                    building_height_m = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    wind_zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    climate_zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fire_safety_class = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    scaffolding_required = table.Column<bool>(type: "boolean", nullable: false),
                    crane_required = table.Column<bool>(type: "boolean", nullable: false),
                    total_area_m2 = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    total_panels = table.Column<int>(type: "integer", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    discount_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tax_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    grand_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    fx_rate_to_base = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    fx_rate_locked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    wind_load_pa_calculated = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    weighted_u_value = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    weighted_sound_db = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    valid_until_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_scene_version = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_work_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    scheduled_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_installer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    machine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workload_m2 = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    checklists_json = table.Column<string>(type: "jsonb", nullable: false),
                    defect_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    recut_count = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_work_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_project_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    length_mm = table.Column<int>(type: "integer", nullable: false),
                    height_mm = table.Column<int>(type: "integer", nullable: false),
                    origin_x = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    origin_y = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    rotation_deg = table.Column<decimal>(type: "numeric(7,3)", nullable: false),
                    profile_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    color_id = table.Column<Guid>(type: "uuid", nullable: true),
                    has_top_drip = table.Column<bool>(type: "boolean", nullable: false),
                    has_bottom_threshold = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_project_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_glass_project_runs_glass_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "glass_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "glass_run_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_a_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_b_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joint_angle_deg = table.Column<decimal>(type: "numeric(7,3)", nullable: false),
                    mitre_cut_deg = table.Column<decimal>(type: "numeric(7,3)", nullable: false),
                    uses_corner_post = table.Column<bool>(type: "boolean", nullable: false),
                    corner_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_run_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_glass_run_connections_glass_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "glass_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "glass_project_panels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    panel_index = table.Column<int>(type: "integer", nullable: false),
                    width_mm = table.Column<int>(type: "integer", nullable: false),
                    opening_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    glass_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    has_handle = table.Column<bool>(type: "boolean", nullable: false),
                    has_lock = table.Column<bool>(type: "boolean", nullable: false),
                    has_brush_seal = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_project_panels", x => x.id);
                    table.ForeignKey(
                        name: "fk_glass_project_panels_glass_project_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "glass_project_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_glass_field_surveys_tenant_id_project_id_status",
                table: "glass_field_surveys",
                columns: new[] { "tenant_id", "project_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_notification_logs_tenant_id_project_id",
                table: "glass_notification_logs",
                columns: new[] { "tenant_id", "project_id" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_notification_logs_tenant_id_status",
                table: "glass_notification_logs",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_attachments_tenant_id_project_id_kind",
                table: "glass_project_attachments",
                columns: new[] { "tenant_id", "project_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_bom_lines_tenant_id_project_id_kind",
                table: "glass_project_bom_lines",
                columns: new[] { "tenant_id", "project_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_change_logs_tenant_id_project_id_created_at_u~",
                table: "glass_project_change_logs",
                columns: new[] { "tenant_id", "project_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_cutting_plans_tenant_id_project_id_plan_type_~",
                table: "glass_project_cutting_plans",
                columns: new[] { "tenant_id", "project_id", "plan_type", "generated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_order_links_tenant_id_order_id",
                table: "glass_project_order_links",
                columns: new[] { "tenant_id", "order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_order_links_tenant_id_project_id",
                table: "glass_project_order_links",
                columns: new[] { "tenant_id", "project_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_panels_run_id",
                table: "glass_project_panels",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_panels_tenant_id_run_id_panel_index",
                table: "glass_project_panels",
                columns: new[] { "tenant_id", "run_id", "panel_index" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_quote_snapshots_tenant_id_project_id_issued_a~",
                table: "glass_project_quote_snapshots",
                columns: new[] { "tenant_id", "project_id", "issued_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_runs_project_id",
                table: "glass_project_runs",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_runs_tenant_id_project_id_order_index",
                table: "glass_project_runs",
                columns: new[] { "tenant_id", "project_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_scenes_tenant_id_project_id_version",
                table: "glass_project_scenes",
                columns: new[] { "tenant_id", "project_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_share_tokens_tenant_id_project_id",
                table: "glass_project_share_tokens",
                columns: new[] { "tenant_id", "project_id" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_share_tokens_token",
                table: "glass_project_share_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_projects_tenant_id_assigned_designer_user_id",
                table: "glass_projects",
                columns: new[] { "tenant_id", "assigned_designer_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_projects_tenant_id_code",
                table: "glass_projects",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_projects_tenant_id_customer_id",
                table: "glass_projects",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_projects_tenant_id_status",
                table: "glass_projects",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_projects_tenant_id_updated_at_utc",
                table: "glass_projects",
                columns: new[] { "tenant_id", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_run_connections_project_id",
                table: "glass_run_connections",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_glass_run_connections_tenant_id_project_id",
                table: "glass_run_connections",
                columns: new[] { "tenant_id", "project_id" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_run_connections_tenant_id_run_a_id_run_b_id",
                table: "glass_run_connections",
                columns: new[] { "tenant_id", "run_a_id", "run_b_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_work_orders_tenant_id_project_id",
                table: "glass_work_orders",
                columns: new[] { "tenant_id", "project_id" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_work_orders_tenant_id_status_scheduled_start_date",
                table: "glass_work_orders",
                columns: new[] { "tenant_id", "status", "scheduled_start_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "glass_field_surveys");

            migrationBuilder.DropTable(
                name: "glass_notification_logs");

            migrationBuilder.DropTable(
                name: "glass_project_attachments");

            migrationBuilder.DropTable(
                name: "glass_project_bom_lines");

            migrationBuilder.DropTable(
                name: "glass_project_change_logs");

            migrationBuilder.DropTable(
                name: "glass_project_cutting_plans");

            migrationBuilder.DropTable(
                name: "glass_project_order_links");

            migrationBuilder.DropTable(
                name: "glass_project_panels");

            migrationBuilder.DropTable(
                name: "glass_project_quote_snapshots");

            migrationBuilder.DropTable(
                name: "glass_project_scenes");

            migrationBuilder.DropTable(
                name: "glass_project_share_tokens");

            migrationBuilder.DropTable(
                name: "glass_run_connections");

            migrationBuilder.DropTable(
                name: "glass_work_orders");

            migrationBuilder.DropTable(
                name: "glass_project_runs");

            migrationBuilder.DropTable(
                name: "glass_projects");
        }
    }
}
