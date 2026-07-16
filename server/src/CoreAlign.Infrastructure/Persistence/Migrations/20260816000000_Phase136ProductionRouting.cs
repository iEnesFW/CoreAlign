using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase136ProductionRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS production_routings (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    code character varying(40) NOT NULL,
    name character varying(200) NOT NULL,
    description character varying(1000) NULL,
    status character varying(20) NOT NULL DEFAULT 'Draft',
    concurrency_token bigint NOT NULL DEFAULT 0,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_production_routings PRIMARY KEY (id),
    CONSTRAINT fk_production_routings_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT ck_production_routings_status CHECK (status IN ('Draft','Active','Archived'))
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_production_routings_tenant_id_code ON production_routings (tenant_id, code);
CREATE INDEX IF NOT EXISTS ix_production_routings_tenant_id_status ON production_routings (tenant_id, status);

CREATE TABLE IF NOT EXISTS routing_steps (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    routing_id uuid NOT NULL,
    step_number integer NOT NULL,
    work_center_id uuid NOT NULL,
    operation_name character varying(100) NOT NULL,
    operation_type character varying(20) NOT NULL DEFAULT 'Other',
    setup_time_minutes numeric(18,4) NOT NULL DEFAULT 0,
    run_time_minutes_per_unit numeric(18,4) NOT NULL DEFAULT 0,
    run_time_minutes_per_sqm numeric(18,4) NULL,
    scrap_percentage numeric(6,3) NOT NULL DEFAULT 0,
    instructions character varying(2000) NULL,
    is_optional boolean NOT NULL DEFAULT false,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_routing_steps PRIMARY KEY (id),
    CONSTRAINT fk_routing_steps_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT fk_routing_steps_production_routings_routing_id FOREIGN KEY (routing_id) REFERENCES production_routings (id) ON DELETE CASCADE,
    CONSTRAINT fk_routing_steps_work_centers_work_center_id FOREIGN KEY (work_center_id) REFERENCES work_centers (id) ON DELETE RESTRICT,
    CONSTRAINT ck_routing_steps_operation_type CHECK (operation_type IN
        ('Cutting','Edging','Tempering','Lamination','Drilling','Sandblasting','Washing','QualityControl','Packaging','Other')),
    CONSTRAINT ck_routing_steps_step_number CHECK (step_number >= 1),
    CONSTRAINT ck_routing_steps_setup_time CHECK (setup_time_minutes >= 0),
    CONSTRAINT ck_routing_steps_run_time_unit CHECK (run_time_minutes_per_unit >= 0),
    CONSTRAINT ck_routing_steps_run_time_sqm CHECK (run_time_minutes_per_sqm IS NULL OR run_time_minutes_per_sqm >= 0),
    CONSTRAINT ck_routing_steps_scrap CHECK (scrap_percentage >= 0 AND scrap_percentage <= 100)
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_routing_steps_tenant_id_routing_id_step_number ON routing_steps (tenant_id, routing_id, step_number);
CREATE INDEX IF NOT EXISTS ix_routing_steps_tenant_id_work_center_id ON routing_steps (tenant_id, work_center_id);
CREATE INDEX IF NOT EXISTS ix_routing_steps_routing_id ON routing_steps (routing_id);
CREATE INDEX IF NOT EXISTS ix_routing_steps_work_center_id ON routing_steps (work_center_id);

CREATE TABLE IF NOT EXISTS work_center_operators (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    work_center_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    qualification_level character varying(20) NOT NULL DEFAULT 'Qualified',
    is_primary boolean NOT NULL DEFAULT false,
    is_active boolean NOT NULL DEFAULT true,
    certified_on date NULL,
    notes character varying(500) NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_work_center_operators PRIMARY KEY (id),
    CONSTRAINT fk_work_center_operators_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT fk_work_center_operators_work_centers_work_center_id FOREIGN KEY (work_center_id) REFERENCES work_centers (id) ON DELETE RESTRICT,
    CONSTRAINT fk_work_center_operators_employees_employee_id FOREIGN KEY (employee_id) REFERENCES employees (id) ON DELETE RESTRICT,
    CONSTRAINT ck_work_center_operators_level CHECK (qualification_level IN ('Trainee','Qualified','Expert'))
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_work_center_operators_tenant_id_work_center_id_employee_id ON work_center_operators (tenant_id, work_center_id, employee_id) WHERE is_active = true;
CREATE UNIQUE INDEX IF NOT EXISTS ix_work_center_operators_tenant_id_work_center_id ON work_center_operators (tenant_id, work_center_id) WHERE is_primary = true AND is_active = true;
CREATE INDEX IF NOT EXISTS ix_work_center_operators_tenant_id_employee_id ON work_center_operators (tenant_id, employee_id);
CREATE INDEX IF NOT EXISTS ix_work_center_operators_work_center_id ON work_center_operators (work_center_id);
CREATE INDEX IF NOT EXISTS ix_work_center_operators_employee_id ON work_center_operators (employee_id);

ALTER TABLE products ADD COLUMN IF NOT EXISTS routing_id uuid NULL;
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_products_production_routings_routing_id') THEN
    ALTER TABLE products ADD CONSTRAINT fk_products_production_routings_routing_id
      FOREIGN KEY (routing_id) REFERENCES production_routings (id) ON DELETE SET NULL NOT VALID;
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_products_routing_id ON products (routing_id);
CREATE INDEX IF NOT EXISTS ix_products_tenant_id_routing_id ON products (tenant_id, routing_id);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS products DROP CONSTRAINT IF EXISTS fk_products_production_routings_routing_id;
DROP INDEX IF EXISTS ix_products_tenant_id_routing_id;
DROP INDEX IF EXISTS ix_products_routing_id;
ALTER TABLE IF EXISTS products DROP COLUMN IF EXISTS routing_id;
DROP TABLE IF EXISTS routing_steps;
DROP TABLE IF EXISTS work_center_operators;
DROP TABLE IF EXISTS production_routings;
");
        }
    }
}
