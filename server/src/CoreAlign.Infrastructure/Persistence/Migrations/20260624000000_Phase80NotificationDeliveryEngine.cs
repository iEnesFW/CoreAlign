using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase80NotificationDeliveryEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_attempts",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 8);

            migrationBuilder.Sql("UPDATE outbox_messages SET max_attempts = 8 WHERE max_attempts = 0;");

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_utc",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notification_rate_counters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scope_key = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    window_start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_rate_counters", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_status_next_attempt_utc",
                table: "outbox_messages",
                columns: new[] { "status", "next_attempt_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_rate_counters_tenant_id_provider_name_scope_sc~",
                table: "notification_rate_counters",
                columns: new[] { "tenant_id", "provider_name", "scope", "scope_key", "window_start_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_rate_counters_window_start_utc",
                table: "notification_rate_counters",
                column: "window_start_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_rate_counters");

            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_status_next_attempt_utc",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "max_attempts",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "next_attempt_utc",
                table: "outbox_messages");
        }
    }
}
