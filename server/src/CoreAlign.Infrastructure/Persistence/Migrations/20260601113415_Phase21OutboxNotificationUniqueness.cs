using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase21OutboxNotificationUniqueness : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""notifications"" a
                USING ""notifications"" b
                WHERE a.""id"" > b.""id""
                  AND a.""recipient_user_id"" = b.""recipient_user_id""
                  AND a.""entity_type"" = b.""entity_type""
                  AND a.""entity_id"" = b.""entity_id""
                  AND a.""type"" = b.""type"";
            ");

            migrationBuilder.CreateIndex(
                name: "ux_notifications_recipient_entity_type",
                table: "notifications",
                columns: new[] { "recipient_user_id", "entity_type", "entity_id", "type" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_notifications_recipient_entity_type",
                table: "notifications");
        }
    }
}
