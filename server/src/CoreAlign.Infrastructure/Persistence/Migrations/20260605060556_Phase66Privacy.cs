using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase66Privacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "username_hash",
                table: "data_subject_requests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "email_hash",
                table: "data_subject_requests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at_utc",
                table: "data_subject_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "concurrency_token",
                table: "data_subject_requests",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "data_export_file_id",
                table: "data_subject_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "data_subject_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "data_subject_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_reason",
                table: "data_subject_requests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "data_subject_requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "legal_basis_override",
                table: "data_subject_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "data_subject_requests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "data_subject_requests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "requester_customer_id",
                table: "data_subject_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "requester_user_id",
                table: "data_subject_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "data_subject_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "submitted_at_utc",
                table: "data_subject_requests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "retention_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    retention_days = table.Column<int>(type: "integer", nullable: false),
                    action_on_expiry = table.Column<int>(type: "integer", nullable: false),
                    last_run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_run_affected_count = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    keep_financial_trail = table.Column<bool>(type: "boolean", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_policies", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_data_subject_requests_tenant_customer",
                table: "data_subject_requests",
                columns: new[] { "tenant_id", "requester_customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_data_subject_requests_tenant_status",
                table: "data_subject_requests",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_retention_policies_tenant_entity",
                table: "retention_policies",
                columns: new[] { "tenant_id", "entity_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "retention_policies");

            migrationBuilder.DropIndex(
                name: "ix_data_subject_requests_tenant_customer",
                table: "data_subject_requests");

            migrationBuilder.DropIndex(
                name: "ix_data_subject_requests_tenant_status",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "completed_at_utc",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "data_export_file_id",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "deleted_reason",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "legal_basis_override",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "requester_customer_id",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "requester_user_id",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "status",
                table: "data_subject_requests");

            migrationBuilder.DropColumn(
                name: "submitted_at_utc",
                table: "data_subject_requests");

            migrationBuilder.AlterColumn<string>(
                name: "username_hash",
                table: "data_subject_requests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "email_hash",
                table: "data_subject_requests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);
        }
    }
}
