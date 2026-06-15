using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase54ReportingScheduling : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    dimensions_json = table.Column<string>(type: "text", nullable: false),
                    measures_json = table.Column<string>(type: "text", nullable: false),
                    filters_json = table.Column<string>(type: "text", nullable: false),
                    sort_by_json = table.Column<string>(type: "text", nullable: true),
                    limit = table.Column<int>(type: "integer", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_definitions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_definitions_tenant_name_unique",
                table: "report_definitions",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_definitions_tenant_entity_type",
                table: "report_definitions",
                columns: new[] { "tenant_id", "entity_type" });

            migrationBuilder.CreateTable(
                name: "report_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    report_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    custom_report_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cron_expression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    recipients_json = table.Column<string>(type: "text", nullable: false),
                    format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    filters_json = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    next_run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_run_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    last_run_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_schedules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_schedules_tenant_due",
                table: "report_schedules",
                columns: new[] { "tenant_id", "is_active", "next_run_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_report_schedules_due",
                table: "report_schedules",
                column: "next_run_at_utc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "report_schedules");
            migrationBuilder.DropTable(name: "report_definitions");
        }
    }
}
