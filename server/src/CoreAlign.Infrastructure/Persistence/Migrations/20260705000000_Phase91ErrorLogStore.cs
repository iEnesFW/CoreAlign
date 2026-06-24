using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase91ErrorLogStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "error_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    trace_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    http_method = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    exception_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    message = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    stack_trace = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    client_page = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    client_component = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    context_json = table.Column<string>(type: "text", nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false),
                    resolution_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_error_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_error_logs_correlation_id",
                table: "error_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_error_logs_is_resolved",
                table: "error_logs",
                column: "is_resolved");

            migrationBuilder.CreateIndex(
                name: "ix_error_logs_occurred_at_utc",
                table: "error_logs",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_error_logs_severity_occurred_at_utc",
                table: "error_logs",
                columns: new[] { "severity", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_error_logs_tenant_id_occurred_at_utc",
                table: "error_logs",
                columns: new[] { "tenant_id", "occurred_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "error_logs");
        }
    }
}
