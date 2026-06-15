using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase59Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "preferred_locale",
                table: "users",
                type: "text",
                nullable: true);

            // require_two_factor_for_roles Phase31'de eklenmis durumda; burada duplicate AddColumn cikariliyor.

            migrationBuilder.AddColumn<DateTime>(
                name: "dealer_rejected_at_utc",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "glass_project_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "substitute_from_product_id",
                table: "order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ip_address_hash",
                table: "login_audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent_hash",
                table: "login_audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "return_request_id",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "anonymized_at_utc",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_anonymized",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ip_address_hash",
                table: "activity_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent_hash",
                table: "activity_logs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customer_merge_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    initiated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    executed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    orders_moved = table.Column<int>(type: "integer", nullable: false),
                    invoices_moved = table.Column<int>(type: "integer", nullable: false),
                    payments_moved = table.Column<int>(type: "integer", nullable: false),
                    addresses_moved = table.Column<int>(type: "integer", nullable: false),
                    contacts_moved = table.Column<int>(type: "integer", nullable: false),
                    comments_moved = table.Column<int>(type: "integer", nullable: false),
                    ledger_entries_moved = table.Column<int>(type: "integer", nullable: false),
                    transactions_moved = table.Column<int>(type: "integer", nullable: false),
                    tag_links_moved = table.Column<int>(type: "integer", nullable: false),
                    dealer_links_moved = table.Column<int>(type: "integer", nullable: false),
                    customer_users_moved = table.Column<int>(type: "integer", nullable: false),
                    other_records_moved = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_merge_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    template_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    recipient_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    body_markdown = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    payload_json = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    category_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    provider_used = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    body_template = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    alt_text = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    uploaded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_images_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    variant_attributes_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    price_override = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    stock_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_variants_products_parent_product_id",
                        column: x => x.parent_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hardware_kit_item",
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
                    table.PrimaryKey("pk_hardware_kit_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_hardware_kit_item_glass_hardware_kits_kit_id",
                        column: x => x.kit_id,
                        principalTable: "glass_hardware_kits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_template_run_preset",
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
                    table.PrimaryKey("pk_project_template_run_preset", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_template_run_preset_project_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "project_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_tenant_id_source_bom_line_id",
                table: "order_lines",
                columns: new[] { "tenant_id", "source_bom_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_tenant_id_substitute_from_product_id",
                table: "order_lines",
                columns: new[] { "tenant_id", "substitute_from_product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_merge_logs_tenant_operation_unique",
                table: "customer_merge_logs",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_merge_logs_tenant_source",
                table: "customer_merge_logs",
                columns: new[] { "tenant_id", "source_customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_merge_logs_tenant_target",
                table: "customer_merge_logs",
                columns: new[] { "tenant_id", "target_customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_hardware_kit_item_kit_id",
                table: "hardware_kit_item",
                column: "kit_id");

            migrationBuilder.CreateIndex(
                name: "ix_hardware_kit_item_tenant_id_hardware_item_id",
                table: "hardware_kit_item",
                columns: new[] { "tenant_id", "hardware_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_hardware_kit_item_tenant_id_kit_id_sort_order",
                table: "hardware_kit_item",
                columns: new[] { "tenant_id", "kit_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_messages_provider_msg",
                table: "notification_messages",
                columns: new[] { "provider_used", "provider_message_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_messages_tenant_category",
                table: "notification_messages",
                columns: new[] { "tenant_id", "category_key" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_messages_tenant_customer",
                table: "notification_messages",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_messages_tenant_status",
                table: "notification_messages",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_messages_tenant_user",
                table: "notification_messages",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_notification_preferences_user_category_channel",
                table: "notification_preferences",
                columns: new[] { "tenant_id", "user_id", "category_key", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_notification_templates_tenant_key_channel_locale",
                table: "notification_templates",
                columns: new[] { "tenant_id", "key", "channel", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id",
                table: "product_images",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_tenant_id_product_id_display_order",
                table: "product_images",
                columns: new[] { "tenant_id", "product_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ux_product_images_tenant_product_primary",
                table: "product_images",
                columns: new[] { "tenant_id", "product_id" },
                unique: true,
                filter: "is_primary = true");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_parent_product_id",
                table: "product_variants",
                column: "parent_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_tenant_active",
                table: "product_variants",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_tenant_parent",
                table: "product_variants",
                columns: new[] { "tenant_id", "parent_product_id" });

            migrationBuilder.CreateIndex(
                name: "ux_product_variants_tenant_parent_sku",
                table: "product_variants",
                columns: new[] { "tenant_id", "parent_product_id", "sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_template_run_preset_template_id",
                table: "project_template_run_preset",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_template_run_preset_tenant_id_template_id_order_ind~",
                table: "project_template_run_preset",
                columns: new[] { "tenant_id", "template_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "ix_provider_webhook_inbox_tenant_signature_hash",
                table: "provider_webhook_inbox",
                columns: new[] { "tenant_id", "signature_hash" },
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_merge_logs");

            migrationBuilder.DropTable(
                name: "hardware_kit_item");

            migrationBuilder.DropTable(
                name: "notification_messages");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "notification_templates");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropTable(
                name: "project_template_run_preset");

            migrationBuilder.DropIndex(
                name: "ix_order_lines_tenant_id_source_bom_line_id",
                table: "order_lines");

            migrationBuilder.DropIndex(
                name: "ix_order_lines_tenant_id_substitute_from_product_id",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "preferred_locale",
                table: "users");

            // require_two_factor_for_roles DropColumn'u Phase31'in Down'unda sahipleniliyor — duplicate cikariliyor.

            migrationBuilder.DropColumn(
                name: "dealer_rejected_at_utc",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "glass_project_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "substitute_from_product_id",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "ip_address_hash",
                table: "login_audit_logs");

            migrationBuilder.DropColumn(
                name: "user_agent_hash",
                table: "login_audit_logs");

            migrationBuilder.DropColumn(
                name: "return_request_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "anonymized_at_utc",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "is_anonymized",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "ip_address_hash",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "user_agent_hash",
                table: "activity_logs");

        }
    }
}
