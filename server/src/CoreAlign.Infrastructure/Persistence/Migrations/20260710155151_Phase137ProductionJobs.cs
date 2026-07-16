using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase137ProductionJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS production_jobs (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    job_number character varying(30) NOT NULL,
    product_id uuid NOT NULL,
    planned_quantity numeric(18,4) NOT NULL,
    completed_quantity numeric(18,4) NOT NULL DEFAULT 0,
    scrapped_quantity numeric(18,4) NOT NULL DEFAULT 0,
    unit_of_measure character varying(20) NOT NULL DEFAULT '',
    status character varying(20) NOT NULL DEFAULT 'Draft',
    source_planned_production_order_id uuid NULL,
    warehouse_id uuid NULL,
    source_routing_id uuid NULL,
    routing_code_snapshot character varying(40) NULL,
    routing_name_snapshot character varying(200) NULL,
    routing_snapshot_version bigint NULL,
    current_step_number integer NULL,
    planned_start_date_utc timestamptz NULL,
    due_date_utc timestamptz NULL,
    released_at_utc timestamptz NULL,
    started_at_utc timestamptz NULL,
    completed_at_utc timestamptz NULL,
    cancelled_at_utc timestamptz NULL,
    cancellation_reason character varying(500) NULL,
    notes character varying(2000) NULL,
    concurrency_token bigint NOT NULL DEFAULT 0,
    created_at_utc timestamptz NOT NULL,
    updated_at_utc timestamptz NOT NULL,
    CONSTRAINT pk_production_jobs PRIMARY KEY (id),
    CONSTRAINT fk_production_jobs_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT fk_production_jobs_products_product_id FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE RESTRICT,
    CONSTRAINT fk_production_jobs_planned_production_orders_source FOREIGN KEY (source_planned_production_order_id) REFERENCES planned_production_orders (id) ON DELETE SET NULL,
    CONSTRAINT fk_production_jobs_warehouses_warehouse_id FOREIGN KEY (warehouse_id) REFERENCES warehouses (id) ON DELETE RESTRICT,
    CONSTRAINT ck_production_jobs_status CHECK (status IN ('Draft','Released','InProgress','OnHold','ReadyToComplete','Completed','Cancelled')),
    CONSTRAINT ck_production_jobs_planned_qty CHECK (planned_quantity > 0),
    CONSTRAINT ck_production_jobs_completed_qty CHECK (completed_quantity >= 0 AND completed_quantity <= planned_quantity),
    CONSTRAINT ck_production_jobs_scrapped_qty CHECK (scrapped_quantity >= 0)
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_production_jobs_tenant_id_job_number ON production_jobs (tenant_id, job_number);
CREATE INDEX IF NOT EXISTS ix_production_jobs_tenant_id_status_due_date_utc_id ON production_jobs (tenant_id, status, due_date_utc, id);
CREATE INDEX IF NOT EXISTS ix_production_jobs_tenant_id_product_id ON production_jobs (tenant_id, product_id);
CREATE INDEX IF NOT EXISTS ix_production_jobs_product_id ON production_jobs (product_id);
CREATE INDEX IF NOT EXISTS ix_production_jobs_source_planned_production_order_id ON production_jobs (source_planned_production_order_id);
CREATE INDEX IF NOT EXISTS ix_production_jobs_warehouse_id ON production_jobs (warehouse_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_production_jobs_source_ppo_active ON production_jobs (tenant_id, source_planned_production_order_id)
    WHERE source_planned_production_order_id IS NOT NULL AND status <> 'Cancelled';

CREATE TABLE IF NOT EXISTS production_job_steps (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    production_job_id uuid NOT NULL,
    step_number integer NOT NULL,
    work_center_id uuid NULL,
    source_routing_step_id uuid NULL,
    operation_name character varying(100) NOT NULL,
    operation_type character varying(20) NOT NULL DEFAULT 'Other',
    setup_time_minutes numeric(18,4) NOT NULL DEFAULT 0,
    run_time_minutes_per_unit numeric(18,4) NOT NULL DEFAULT 0,
    run_time_minutes_per_sqm numeric(18,4) NULL,
    scrap_percentage numeric(6,3) NOT NULL DEFAULT 0,
    instructions character varying(2000) NULL,
    is_optional boolean NOT NULL DEFAULT false,
    status character varying(20) NOT NULL DEFAULT 'Pending',
    input_quantity numeric(18,4) NOT NULL DEFAULT 0,
    assigned_operator_id uuid NULL,
    started_at_utc timestamptz NULL,
    finished_at_utc timestamptz NULL,
    actual_setup_minutes numeric(18,4) NULL,
    actual_run_minutes numeric(18,4) NULL,
    good_quantity numeric(18,4) NOT NULL DEFAULT 0,
    scrapped_quantity numeric(18,4) NOT NULL DEFAULT 0,
    scrap_reason_code_id uuid NULL,
    reworked_from_step_number integer NULL,
    rework_count integer NOT NULL DEFAULT 0,
    notes character varying(1000) NULL,
    created_at_utc timestamptz NOT NULL,
    updated_at_utc timestamptz NOT NULL,
    CONSTRAINT pk_production_job_steps PRIMARY KEY (id),
    CONSTRAINT fk_production_job_steps_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT fk_production_job_steps_production_jobs_production_job_id FOREIGN KEY (production_job_id) REFERENCES production_jobs (id) ON DELETE CASCADE,
    CONSTRAINT fk_production_job_steps_work_centers_work_center_id FOREIGN KEY (work_center_id) REFERENCES work_centers (id) ON DELETE SET NULL,
    CONSTRAINT ck_production_job_steps_status CHECK (status IN ('Pending','InProgress','Completed','Skipped','Reopened')),
    CONSTRAINT ck_production_job_steps_optype CHECK (operation_type IN
        ('Cutting','Edging','Tempering','Lamination','Drilling','Sandblasting','Washing','QualityControl','Packaging','Other')),
    CONSTRAINT ck_production_job_steps_step_no CHECK (step_number >= 1),
    CONSTRAINT ck_production_job_steps_times CHECK (setup_time_minutes >= 0 AND run_time_minutes_per_unit >= 0
        AND (run_time_minutes_per_sqm IS NULL OR run_time_minutes_per_sqm >= 0)
        AND (actual_setup_minutes IS NULL OR actual_setup_minutes >= 0)
        AND (actual_run_minutes IS NULL OR actual_run_minutes >= 0)),
    CONSTRAINT ck_production_job_steps_scrap_pct CHECK (scrap_percentage >= 0 AND scrap_percentage <= 100),
    CONSTRAINT ck_production_job_steps_qty CHECK (input_quantity >= 0 AND good_quantity >= 0 AND scrapped_quantity >= 0),
    CONSTRAINT ck_production_job_steps_rework CHECK (rework_count >= 0),
    CONSTRAINT ck_production_job_steps_synthetic_wc CHECK (work_center_id IS NOT NULL OR source_routing_step_id IS NULL)
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_production_job_steps_tenant_id_production_job_id_step_number ON production_job_steps (tenant_id, production_job_id, step_number);
CREATE INDEX IF NOT EXISTS ix_production_job_steps_production_job_id ON production_job_steps (production_job_id);
CREATE INDEX IF NOT EXISTS ix_production_job_steps_tenant_id_status ON production_job_steps (tenant_id, status);
CREATE INDEX IF NOT EXISTS ix_production_job_steps_tenant_id_work_center_id ON production_job_steps (tenant_id, work_center_id);
CREATE INDEX IF NOT EXISTS ix_production_job_steps_work_center_id ON production_job_steps (work_center_id);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS production_job_steps;
DROP TABLE IF EXISTS production_jobs;
");
        }
    }
}
