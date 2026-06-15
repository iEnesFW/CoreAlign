using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase55InstallationAcceptance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "installation_acceptances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inspector_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_signature_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_signature_captured_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    checklist_json = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    photo_file_ids = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    notes_md = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installation_acceptances", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_installation_acceptances_tenant_workorder",
                table: "installation_acceptances",
                columns: new[] { "tenant_id", "work_order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_installation_acceptances_tenant_project",
                table: "installation_acceptances",
                columns: new[] { "tenant_id", "project_id" });

            migrationBuilder.CreateIndex(
                name: "ix_installation_acceptances_tenant_customer",
                table: "installation_acceptances",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_installation_acceptances_tenant_inspector",
                table: "installation_acceptances",
                columns: new[] { "tenant_id", "inspector_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_installation_acceptances_tenant_status",
                table: "installation_acceptances",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateTable(
                name: "punch_list_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    acceptance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_punch_list_items", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_punch_list_items_tenant_acceptance",
                table: "punch_list_items",
                columns: new[] { "tenant_id", "acceptance_id" });

            migrationBuilder.CreateIndex(
                name: "ix_punch_list_items_tenant_status",
                table: "punch_list_items",
                columns: new[] { "tenant_id", "status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "punch_list_items");
            migrationBuilder.DropTable(name: "installation_acceptances");
        }
    }
}
