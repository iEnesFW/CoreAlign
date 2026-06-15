using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase63BI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dashboard_widgets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    data_source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    query_config_json = table.Column<string>(type: "text", nullable: false),
                    grid_x = table.Column<int>(type: "integer", nullable: false),
                    grid_y = table.Column<int>(type: "integer", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dashboard_widgets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    saved_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ran_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ran_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    result_row_count = table.Column<int>(type: "integer", nullable: false),
                    export_format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "saved_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    data_source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    query_config_json = table.Column<string>(type: "text", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    last_run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_run_row_count = table.Column<int>(type: "integer", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_reports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dashboard_widgets_tenant_user_active",
                table: "dashboard_widgets",
                columns: new[] { "tenant_id", "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_report_runs_tenant_saved_at",
                table: "report_runs",
                columns: new[] { "tenant_id", "saved_report_id", "ran_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_saved_reports_tenant_owner",
                table: "saved_reports",
                columns: new[] { "tenant_id", "owner_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_saved_reports_tenant_public",
                table: "saved_reports",
                columns: new[] { "tenant_id", "is_public" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dashboard_widgets");

            migrationBuilder.DropTable(
                name: "report_runs");

            migrationBuilder.DropTable(
                name: "saved_reports");
        }
    }
}
