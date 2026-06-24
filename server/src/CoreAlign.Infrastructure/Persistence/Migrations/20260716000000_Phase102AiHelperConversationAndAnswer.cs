using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase102AiHelperConversationAndAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "answer_text",
                table: "ai_helper_query_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "conversation_id",
                table: "ai_helper_query_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_ai_helper_query_logs_conversation_id",
                table: "ai_helper_query_logs",
                column: "conversation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ai_helper_query_logs_conversation_id",
                table: "ai_helper_query_logs");

            migrationBuilder.DropColumn(
                name: "answer_text",
                table: "ai_helper_query_logs");

            migrationBuilder.DropColumn(
                name: "conversation_id",
                table: "ai_helper_query_logs");
        }
    }
}
