using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGlassEnclosureSchema_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "climate_zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name_tr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    avg_winter_temperature_c = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    avg_humidity_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    corrosion_class = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    recommends_double_glazing = table.Column<bool>(type: "boolean", nullable: false),
                    recommends_corrosion_resistant_coating = table.Column<bool>(type: "boolean", nullable: false),
                    recommends_seismic_smaller_panel = table.Column<bool>(type: "boolean", nullable: false),
                    il_postal_prefix_list_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_climate_zones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_brand_vendors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    default_payment_terms = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_preferred = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_brand_vendors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_color_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ral_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    hex_color = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    finish_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price_modifier_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_color_options", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_discount_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    scope = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    customer_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    coupon_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    min_area_m2 = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    valid_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    discount_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    stackable = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_discount_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_enclosure_settings_store",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_stock_bar_length_mm = table.Column<int>(type: "integer", nullable: false),
                    default_jumbo_glass_width_mm = table.Column<int>(type: "integer", nullable: false),
                    default_jumbo_glass_height_mm = table.Column<int>(type: "integer", nullable: false),
                    saw_kerf_mm = table.Column<decimal>(type: "numeric(8,3)", nullable: false),
                    glass_kerf_mm = table.Column<decimal>(type: "numeric(8,3)", nullable: false),
                    guillotine_required = table.Column<bool>(type: "boolean", nullable: false),
                    default_waste_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    labor_cost_per_m2 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    default_margin_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    field_tolerance_top_mm = table.Column<int>(type: "integer", nullable: false),
                    field_tolerance_side_mm = table.Column<int>(type: "integer", nullable: false),
                    transport_rate_per_km = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    transport_rate_per_kg = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    scaffolding_required_from_floor = table.Column<int>(type: "integer", nullable: false),
                    scaffolding_rate_per_m2 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    crane_required_from_floor = table.Column<int>(type: "integer", nullable: false),
                    crane_rate_per_meter = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    workshop_daily_capacity_m2 = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    default_payment_terms_json = table.Column<string>(type: "jsonb", nullable: false),
                    default_locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    default_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    data_retention_days = table.Column<int>(type: "integer", nullable: false),
                    whatsapp_business_phone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notification_email_from = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    quote_share_token_ttl_days = table.Column<int>(type: "integer", nullable: false),
                    onboarding_complete = table.Column<bool>(type: "boolean", nullable: false),
                    onboarding_state_json = table.Column<string>(type: "jsonb", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_enclosure_settings_store", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_hardware_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    compatible_system_ids_json = table.Column<string>(type: "jsonb", nullable: false),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    max_load_kg = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    model_glb_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    preferred_vendor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vendor_part_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    reorder_point_quantity = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    linked_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_hardware_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_hardware_kits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_hardware_kits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_notification_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    event_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    subject_template = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    body_template = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_notification_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_profile_systems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    max_panel_width_mm = table.Column<int>(type: "integer", nullable: false),
                    max_panel_height_mm = table.Column<int>(type: "integer", nullable: false),
                    max_panel_weight_kg = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    supported_glass_thicknesses_json = table.Column<string>(type: "jsonb", nullable: false),
                    supported_openings_json = table.Column<string>(type: "jsonb", nullable: false),
                    certification_class = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fire_class = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    thermal_u_value = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    thermal_break_factor = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_profile_systems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    thickness_mm = table.Column<int>(type: "integer", nullable: false),
                    structure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    glass_layers_json = table.Column<string>(type: "jsonb", nullable: false),
                    u_value = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    sound_db = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    max_panel_area_m2 = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    allowable_pressure_pa = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    weight_kg_per_m2 = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    price_per_m2 = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    linked_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wind_zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    region_label_tr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    region_label_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    base_wind_pressure_pa = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    height_factor_multiplier = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    is_coastal = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wind_zones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glass_hardware_kit_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hardware_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_formula = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    condition_expression = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_hardware_kit_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_glass_hardware_kit_items_glass_hardware_kits_kit_id",
                        column: x => x.kit_id,
                        principalTable: "glass_hardware_kits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "glass_profile_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    stock_bar_length_mm = table.Column<int>(type: "integer", nullable: false),
                    weight_kg_per_meter = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    cross_section_svg = table.Column<string>(type: "text", nullable: true),
                    cross_section_dxf_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    parametric_description_json = table.Column<string>(type: "jsonb", nullable: true),
                    default_color_id = table.Column<Guid>(type: "uuid", nullable: true),
                    preferred_vendor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vendor_part_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    reorder_point_meters = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    price_per_kg = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    linked_product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_profile_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_glass_profile_items_glass_profile_systems_system_id",
                        column: x => x.system_id,
                        principalTable: "glass_profile_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_climate_zones_code",
                table: "climate_zones",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_climate_zones_is_active",
                table: "climate_zones",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_glass_brand_vendors_tenant_id_brand_id_is_preferred",
                table: "glass_brand_vendors",
                columns: new[] { "tenant_id", "brand_id", "is_preferred" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_brand_vendors_tenant_id_brand_id_vendor_id",
                table: "glass_brand_vendors",
                columns: new[] { "tenant_id", "brand_id", "vendor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_color_options_tenant_id_code",
                table: "glass_color_options",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_color_options_tenant_id_is_active",
                table: "glass_color_options",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_discount_rules_tenant_id_code",
                table: "glass_discount_rules",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_discount_rules_tenant_id_coupon_code",
                table: "glass_discount_rules",
                columns: new[] { "tenant_id", "coupon_code" },
                unique: true,
                filter: "coupon_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_glass_discount_rules_tenant_id_scope_is_active",
                table: "glass_discount_rules",
                columns: new[] { "tenant_id", "scope", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_enclosure_settings_store_tenant_id",
                table: "glass_enclosure_settings_store",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_hardware_items_tenant_id_brand_id",
                table: "glass_hardware_items",
                columns: new[] { "tenant_id", "brand_id" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_hardware_items_tenant_id_category_is_active",
                table: "glass_hardware_items",
                columns: new[] { "tenant_id", "category", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_hardware_items_tenant_id_code",
                table: "glass_hardware_items",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_hardware_kit_items_kit_id",
                table: "glass_hardware_kit_items",
                column: "kit_id");

            migrationBuilder.CreateIndex(
                name: "ix_glass_hardware_kit_items_tenant_id_hardware_item_id",
                table: "glass_hardware_kit_items",
                columns: new[] { "tenant_id", "hardware_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_hardware_kit_items_tenant_id_kit_id_sort_order",
                table: "glass_hardware_kit_items",
                columns: new[] { "tenant_id", "kit_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_hardware_kits_tenant_id_code",
                table: "glass_hardware_kits",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_hardware_kits_tenant_id_system_id",
                table: "glass_hardware_kits",
                columns: new[] { "tenant_id", "system_id" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_notification_templates_tenant_id_code",
                table: "glass_notification_templates",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_notification_templates_tenant_id_event_code_channel_l~",
                table: "glass_notification_templates",
                columns: new[] { "tenant_id", "event_code", "channel", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_profile_items_system_id",
                table: "glass_profile_items",
                column: "system_id");

            migrationBuilder.CreateIndex(
                name: "ix_glass_profile_items_tenant_id_code",
                table: "glass_profile_items",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_profile_items_tenant_id_system_id_role",
                table: "glass_profile_items",
                columns: new[] { "tenant_id", "system_id", "role" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_profile_systems_tenant_id_brand_id",
                table: "glass_profile_systems",
                columns: new[] { "tenant_id", "brand_id" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_profile_systems_tenant_id_code",
                table: "glass_profile_systems",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_profile_systems_tenant_id_system_type_is_active",
                table: "glass_profile_systems",
                columns: new[] { "tenant_id", "system_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_types_tenant_id_code",
                table: "glass_types",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_types_tenant_id_structure_is_active",
                table: "glass_types",
                columns: new[] { "tenant_id", "structure", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_wind_zones_code",
                table: "wind_zones",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wind_zones_is_active",
                table: "wind_zones",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "climate_zones");

            migrationBuilder.DropTable(
                name: "glass_brand_vendors");

            migrationBuilder.DropTable(
                name: "glass_color_options");

            migrationBuilder.DropTable(
                name: "glass_discount_rules");

            migrationBuilder.DropTable(
                name: "glass_enclosure_settings_store");

            migrationBuilder.DropTable(
                name: "glass_hardware_items");

            migrationBuilder.DropTable(
                name: "glass_hardware_kit_items");

            migrationBuilder.DropTable(
                name: "glass_notification_templates");

            migrationBuilder.DropTable(
                name: "glass_profile_items");

            migrationBuilder.DropTable(
                name: "glass_types");

            migrationBuilder.DropTable(
                name: "wind_zones");

            migrationBuilder.DropTable(
                name: "glass_hardware_kits");

            migrationBuilder.DropTable(
                name: "glass_profile_systems");

        }
    }
}
