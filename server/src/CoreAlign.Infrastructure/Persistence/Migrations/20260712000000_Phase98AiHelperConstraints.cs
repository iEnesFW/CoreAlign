using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase98AiHelperConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_ai_kb_documents_tenant_id",
                table: "ai_kb_documents",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_ai_kb_documents_scope",
                table: "ai_kb_documents",
                sql: "scope IN ('Public','Tenant','Role')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_ai_kb_documents_source_type",
                table: "ai_kb_documents",
                sql: "source_type IN ('Route','I18n','ModuleDoc','Article','Sector')");

            migrationBuilder.CreateIndex(
                name: "ix_ai_kb_chunks_tenant_id",
                table: "ai_kb_chunks",
                column: "tenant_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_ai_kb_chunks_scope",
                table: "ai_kb_chunks",
                sql: "scope IN ('Public','Tenant','Role')");

            migrationBuilder.AddForeignKey(
                name: "fk_ai_kb_chunks_tenants_tenant_id",
                table: "ai_kb_chunks",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ai_kb_documents_tenants_tenant_id",
                table: "ai_kb_documents",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ai_kb_chunks_tenants_tenant_id",
                table: "ai_kb_chunks");

            migrationBuilder.DropForeignKey(
                name: "fk_ai_kb_documents_tenants_tenant_id",
                table: "ai_kb_documents");

            migrationBuilder.DropIndex(
                name: "ix_ai_kb_documents_tenant_id",
                table: "ai_kb_documents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_ai_kb_documents_scope",
                table: "ai_kb_documents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_ai_kb_documents_source_type",
                table: "ai_kb_documents");

            migrationBuilder.DropIndex(
                name: "ix_ai_kb_chunks_tenant_id",
                table: "ai_kb_chunks");

            migrationBuilder.DropCheckConstraint(
                name: "ck_ai_kb_chunks_scope",
                table: "ai_kb_chunks");
        }
    }
}
