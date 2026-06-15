using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase16BillingAndModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "modules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    icon_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_core = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    gateway_intent_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    payment_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    paid_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_modules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_modules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "module_price_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_module_price_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_module_price_plans_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    intent_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    raw_response_json = table.Column<string>(type: "jsonb", nullable: true),
                    attempted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_attempts_subscription_orders_subscription_order_id",
                        column: x => x.subscription_order_id,
                        principalTable: "subscription_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscription_order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    module_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    plan_label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_order_items_subscription_orders_subscription_o~",
                        column: x => x.subscription_order_id,
                        principalTable: "subscription_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_module_price_plans_module_id_code",
                table: "module_price_plans",
                columns: new[] { "module_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_module_price_plans_module_id_is_active_sort_order",
                table: "module_price_plans",
                columns: new[] { "module_id", "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_modules_code",
                table: "modules",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_modules_is_active_sort_order",
                table: "modules",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempts_subscription_order_id",
                table: "payment_attempts",
                column: "subscription_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_attempts_tenant_id_subscription_order_id",
                table: "payment_attempts",
                columns: new[] { "tenant_id", "subscription_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_order_items_subscription_order_id",
                table: "subscription_order_items",
                column: "subscription_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_order_items_tenant_id_subscription_order_id",
                table: "subscription_order_items",
                columns: new[] { "tenant_id", "subscription_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_orders_gateway_intent",
                table: "subscription_orders",
                columns: new[] { "gateway_name", "gateway_intent_id" },
                filter: "gateway_intent_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_orders_tenant_id_order_number",
                table: "subscription_orders",
                columns: new[] { "tenant_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscription_orders_tenant_id_status_created_at_utc",
                table: "subscription_orders",
                columns: new[] { "tenant_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_modules_tenant_id_end_utc",
                table: "tenant_modules",
                columns: new[] { "tenant_id", "end_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_modules_tenant_id_module_id",
                table: "tenant_modules",
                columns: new[] { "tenant_id", "module_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "module_price_plans");

            migrationBuilder.DropTable(
                name: "payment_attempts");

            migrationBuilder.DropTable(
                name: "subscription_order_items");

            migrationBuilder.DropTable(
                name: "tenant_modules");

            migrationBuilder.DropTable(
                name: "modules");

            migrationBuilder.DropTable(
                name: "subscription_orders");
        }
    }
}
