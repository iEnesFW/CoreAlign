using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase103FeedbackAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE feedback_tickets ADD COLUMN IF NOT EXISTS attachment_content_type text NULL;");
            migrationBuilder.Sql("ALTER TABLE feedback_tickets ADD COLUMN IF NOT EXISTS attachment_file_name text NULL;");
            migrationBuilder.Sql("ALTER TABLE feedback_tickets ADD COLUMN IF NOT EXISTS attachment_path text NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE feedback_tickets DROP COLUMN IF EXISTS attachment_content_type;");
            migrationBuilder.Sql("ALTER TABLE feedback_tickets DROP COLUMN IF EXISTS attachment_file_name;");
            migrationBuilder.Sql("ALTER TABLE feedback_tickets DROP COLUMN IF EXISTS attachment_path;");
        }
    }
}
