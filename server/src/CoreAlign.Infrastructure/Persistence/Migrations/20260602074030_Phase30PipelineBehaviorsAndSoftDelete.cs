using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase30PipelineBehaviorsAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "concurrency_token",
                table: "orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "concurrency_token",
                table: "glass_work_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "glass_work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "glass_work_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_reason",
                table: "glass_work_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "glass_work_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "glass_projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "glass_projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_reason",
                table: "glass_projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "glass_projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "concurrency_token",
                table: "glass_project_runs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "concurrency_token",
                table: "glass_project_panels",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "glass_field_surveys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "glass_field_surveys",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_reason",
                table: "glass_field_surveys",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "glass_field_surveys",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_reason",
                table: "customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "glass_work_orders");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "glass_work_orders");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "glass_work_orders");

            migrationBuilder.DropColumn(
                name: "deleted_reason",
                table: "glass_work_orders");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "glass_work_orders");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "deleted_reason",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "glass_projects");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "glass_project_runs");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "glass_project_panels");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "glass_field_surveys");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "glass_field_surveys");

            migrationBuilder.DropColumn(
                name: "deleted_reason",
                table: "glass_field_surveys");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "glass_field_surveys");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "deleted_reason",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "customers");
        }
    }
}
