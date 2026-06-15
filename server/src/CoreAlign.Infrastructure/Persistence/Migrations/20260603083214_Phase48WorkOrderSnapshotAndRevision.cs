using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase48WorkOrderSnapshotAndRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bom_snapshot_json",
                table: "glass_work_orders",
                type: "character varying(64000)",
                maxLength: 64000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "bom_snapshot_total",
                table: "glass_work_orders",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cutting_plan1d_id",
                table: "glass_work_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cutting_plan2d_id",
                table: "glass_work_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_outstanding_blocking_revision",
                table: "glass_work_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "revision_count",
                table: "glass_work_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "glass_work_order_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_number = table.Column<int>(type: "integer", nullable: false),
                    previous_snapshot_json = table.Column<string>(type: "character varying(64000)", maxLength: 64000, nullable: true),
                    new_snapshot_json = table.Column<string>(type: "character varying(64000)", maxLength: 64000, nullable: false),
                    delta_json = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    delta_percent = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    override_reason = table.Column<string>(type: "text", nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glass_work_order_revisions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_with_snapshot",
                table: "glass_work_orders",
                column: "tenant_id",
                filter: "bom_snapshot_json IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_glass_work_order_revisions_tenant_id_status",
                table: "glass_work_order_revisions",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_glass_work_order_revisions_work_order_id_revision_number",
                table: "glass_work_order_revisions",
                columns: new[] { "work_order_id", "revision_number" },
                unique: true);

            migrationBuilder.Sql("ALTER TABLE glass_work_order_revisions ADD CONSTRAINT ck_glass_work_order_revisions_status CHECK (status IN ('SilentSnapshot','PendingApproval','Approved','Rejected','Blocked'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE glass_work_order_revisions DROP CONSTRAINT ck_glass_work_order_revisions_status;");

            migrationBuilder.DropTable(
                name: "glass_work_order_revisions");

            migrationBuilder.DropIndex(
                name: "ix_work_orders_with_snapshot",
                table: "glass_work_orders");

            migrationBuilder.DropColumn(
                name: "bom_snapshot_json",
                table: "glass_work_orders");

            migrationBuilder.DropColumn(
                name: "bom_snapshot_total",
                table: "glass_work_orders");

            migrationBuilder.DropColumn(
                name: "cutting_plan1d_id",
                table: "glass_work_orders");

            migrationBuilder.DropColumn(
                name: "cutting_plan2d_id",
                table: "glass_work_orders");

            migrationBuilder.DropColumn(
                name: "has_outstanding_blocking_revision",
                table: "glass_work_orders");

            migrationBuilder.DropColumn(
                name: "revision_count",
                table: "glass_work_orders");
        }
    }
}
