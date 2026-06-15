using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// MRP T6 Rough-Cut Capacity Planning (Phase 76). Idempotent: re-applying is a
    /// no-op via IF NOT EXISTS. Introduces the work_centers table (a minimal,
    /// single-operation routing capacity bucket) and two routing columns on products
    /// (work_center_id, run_time_minutes_per_unit). The run-time default 0 backfills
    /// existing rows so an unrouted Make product contributes zero load. Tenant-scoped
    /// unique index on (tenant_id, code) mirrors the other manufacturing tables.
    /// </summary>
    public partial class Phase76WorkCenterRouting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS work_centers (
    id uuid NOT NULL,
    code character varying(32) NOT NULL,
    name character varying(128) NOT NULL,
    daily_capacity_minutes numeric(18,4) NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_work_centers PRIMARY KEY (id)
);
");

            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX IF NOT EXISTS ix_work_centers_tenant_code_unique ON work_centers (tenant_id, code);
CREATE INDEX IF NOT EXISTS ix_work_centers_tenant_id_is_active ON work_centers (tenant_id, is_active);
");

            migrationBuilder.Sql(@"
ALTER TABLE products ADD COLUMN IF NOT EXISTS work_center_id uuid NULL;
ALTER TABLE products ADD COLUMN IF NOT EXISTS run_time_minutes_per_unit numeric(18,4) NOT NULL DEFAULT 0;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE products DROP COLUMN IF EXISTS run_time_minutes_per_unit;
ALTER TABLE products DROP COLUMN IF EXISTS work_center_id;
");

            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS work_centers;
");
        }
    }
}
