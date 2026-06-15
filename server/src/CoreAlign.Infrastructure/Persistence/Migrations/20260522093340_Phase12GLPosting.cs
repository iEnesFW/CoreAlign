using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12GLPosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_document_id",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_document_number",
                table: "journal_entries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                table: "journal_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "gl_posting_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    posting_key = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    account_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gl_posting_mappings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_tenant_id_source_type_source_document_id",
                table: "journal_entries",
                columns: new[] { "tenant_id", "source_type", "source_document_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gl_posting_mappings_tenant_id_posting_key",
                table: "gl_posting_mappings",
                columns: new[] { "tenant_id", "posting_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gl_posting_mappings");

            migrationBuilder.DropIndex(
                name: "ix_journal_entries_tenant_id_source_type_source_document_id",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "source_document_id",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "source_document_number",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "source_type",
                table: "journal_entries");
        }
    }
}
