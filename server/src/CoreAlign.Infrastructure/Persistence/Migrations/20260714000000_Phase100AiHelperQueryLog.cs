using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase100AiHelperQueryLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_helper_query_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    question = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    route_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    chat_model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    chunk_count = table.Column<int>(type: "integer", nullable: false),
                    top_score = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    retrieved_json = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_helper_query_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_helper_query_logs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_helper_query_logs_created_at_utc",
                table: "ai_helper_query_logs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_ai_helper_query_logs_tenant_id",
                table: "ai_helper_query_logs",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_helper_query_logs");
        }
    }
}
