using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CoreAlignDbContext))]
    [Migration("20260713000000_Phase99AiHelperRoleScope")]
    public partial class Phase99AiHelperRoleScope : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE ai_kb_documents ADD COLUMN IF NOT EXISTS required_role character varying(64) NULL;");
            migrationBuilder.Sql(
                "ALTER TABLE ai_kb_chunks ADD COLUMN IF NOT EXISTS required_role character varying(64) NULL;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_ai_kb_documents_role_requires_role') THEN
        ALTER TABLE ai_kb_documents
            ADD CONSTRAINT ck_ai_kb_documents_role_requires_role
            CHECK (scope <> 'Role' OR required_role IS NOT NULL);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_ai_kb_chunks_role_requires_role') THEN
        ALTER TABLE ai_kb_chunks
            ADD CONSTRAINT ck_ai_kb_chunks_role_requires_role
            CHECK (scope <> 'Role' OR required_role IS NOT NULL);
    END IF;
END
$$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE ai_kb_documents DROP CONSTRAINT IF EXISTS ck_ai_kb_documents_role_requires_role;");
            migrationBuilder.Sql(
                "ALTER TABLE ai_kb_chunks DROP CONSTRAINT IF EXISTS ck_ai_kb_chunks_role_requires_role;");
            migrationBuilder.Sql(
                "ALTER TABLE ai_kb_documents DROP COLUMN IF EXISTS required_role;");
            migrationBuilder.Sql(
                "ALTER TABLE ai_kb_chunks DROP COLUMN IF EXISTS required_role;");
        }
    }
}
