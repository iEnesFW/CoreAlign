using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase47PersonaModeAndUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "default_ux_complexity_mode",
                table: "tenants",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_submitted_snapshot_json",
                table: "orders",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_number = table.Column<int>(type: "integer", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_persona = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    proposed_lines = table.Column<string>(type: "jsonb", nullable: false),
                    counterparty_decision_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    request_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_revisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_revisions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode_override = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    per_screen_overrides_json = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    locale_override = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    theme_override = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_preferences", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_revisions_order_id",
                table: "order_revisions",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_revisions_tenant_id_order_id_revision_number",
                table: "order_revisions",
                columns: new[] { "tenant_id", "order_id", "revision_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_revisions_tenant_id_order_id_status",
                table: "order_revisions",
                columns: new[] { "tenant_id", "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_order_revisions_tenant_id_status_requested_at_utc",
                table: "order_revisions",
                columns: new[] { "tenant_id", "status", "requested_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_user_preferences_user_id",
                table: "user_preferences",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_revisions");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropColumn(
                name: "default_ux_complexity_mode",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "original_submitted_snapshot_json",
                table: "orders");
        }
    }
}
