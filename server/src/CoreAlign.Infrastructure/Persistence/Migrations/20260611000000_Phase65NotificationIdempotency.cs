using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase65NotificationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_hash",
                table: "notification_messages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_notification_messages_tenant_idempotency",
                table: "notification_messages",
                columns: new[] { "tenant_id", "idempotency_hash" },
                unique: true,
                filter: "idempotency_hash <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_notification_messages_tenant_idempotency",
                table: "notification_messages");

            migrationBuilder.DropColumn(
                name: "idempotency_hash",
                table: "notification_messages");
        }
    }
}
