using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase105AiKbSourceCodeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE ai_kb_documents DROP CONSTRAINT IF EXISTS ck_ai_kb_documents_source_type;");
            migrationBuilder.Sql("ALTER TABLE ai_kb_documents ADD CONSTRAINT ck_ai_kb_documents_source_type CHECK (source_type IN ('Route','I18n','ModuleDoc','Article','Sector','SourceCode'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE ai_kb_documents DROP CONSTRAINT IF EXISTS ck_ai_kb_documents_source_type;");
            migrationBuilder.Sql("ALTER TABLE ai_kb_documents ADD CONSTRAINT ck_ai_kb_documents_source_type CHECK (source_type IN ('Route','I18n','ModuleDoc','Article','Sector'));");
        }
    }
}
