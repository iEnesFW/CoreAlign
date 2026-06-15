using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase22NotificationIndexTenantScoped : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_notifications_recipient_entity_type",
                table: "notifications");

            migrationBuilder.CreateIndex(
                name: "ux_notifications_tenant_recipient_entity_type",
                table: "notifications",
                columns: new[] { "tenant_id", "recipient_user_id", "entity_type", "entity_id", "type" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_notifications_tenant_recipient_entity_type",
                table: "notifications");

            migrationBuilder.CreateIndex(
                name: "ux_notifications_recipient_entity_type",
                table: "notifications",
                columns: new[] { "recipient_user_id", "entity_type", "entity_id", "type" },
                unique: true);
        }
    }
}
