using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase110NotificationMessageAcknowledge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE notification_messages ADD COLUMN IF NOT EXISTS acknowledged_at_utc timestamp with time zone NULL;");
            migrationBuilder.Sql("ALTER TABLE notification_messages ADD COLUMN IF NOT EXISTS acknowledged_by_user_id uuid NULL;");
            migrationBuilder.Sql("ALTER TABLE notification_messages ADD COLUMN IF NOT EXISTS acknowledgment_note character varying(2000) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE notification_messages DROP COLUMN IF EXISTS acknowledgment_note;");
            migrationBuilder.Sql("ALTER TABLE notification_messages DROP COLUMN IF EXISTS acknowledged_by_user_id;");
            migrationBuilder.Sql("ALTER TABLE notification_messages DROP COLUMN IF EXISTS acknowledged_at_utc;");
        }
    }
}
