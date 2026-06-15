using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// MRP planning persistence (Tranche T1, Group B). Idempotent: re-applying is a
    /// no-op via IF NOT EXISTS. The 7 product planning columns are additive; the four
    /// mrp_* tables hold committed plan runs, planned orders, action messages and pegging.
    /// CoreAlignDbContextModelSnapshot.cs is intentionally NOT touched (parallel-agent
    /// guard); snapshot reconcile is tracked as ERP-MRP-001 in docs/mrp-blockers.md.
    /// </summary>
    public partial class Phase72MrpPlanning : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE products ADD COLUMN IF NOT EXISTS lot_sizing_policy integer NOT NULL DEFAULT 2;
ALTER TABLE products ADD COLUMN IF NOT EXISTS fixed_order_quantity numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS order_multiple numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS eoq_annual_demand numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS ordering_cost numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS holding_cost_rate numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE products ADD COLUMN IF NOT EXISTS service_level_target numeric(18,4) NOT NULL DEFAULT 0;
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mrp_plan_runs (
    id uuid NOT NULL,
    number character varying(32) NOT NULL,
    status character varying(20) NOT NULL,
    as_of_date_utc timestamp with time zone NOT NULL,
    bucket_kind character varying(10) NOT NULL,
    horizon_days integer NOT NULL,
    idempotency_key character varying(64) NOT NULL,
    products_evaluated integer NOT NULL,
    planned_order_count integer NOT NULL,
    action_message_count integer NOT NULL,
    created_by_user_id uuid NOT NULL,
    committed_at_utc timestamp with time zone NULL,
    concurrency_token bigint NOT NULL DEFAULT 0,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_mrp_plan_runs PRIMARY KEY (id)
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mrp_planned_orders (
    id uuid NOT NULL,
    plan_run_id uuid NOT NULL,
    product_id uuid NOT NULL,
    low_level_code integer NOT NULL,
    quantity numeric(18,4) NOT NULL,
    due_date_utc timestamp with time zone NOT NULL,
    release_date_utc timestamp with time zone NOT NULL,
    preferred_supplier_id uuid NULL,
    estimated_unit_cost numeric(18,4) NOT NULL,
    source_policy character varying(30) NOT NULL,
    is_firmed boolean NOT NULL,
    is_released boolean NOT NULL,
    converted_requisition_id uuid NULL,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_mrp_planned_orders PRIMARY KEY (id),
    CONSTRAINT fk_mrp_planned_orders_mrp_plan_runs_plan_run_id FOREIGN KEY (plan_run_id) REFERENCES mrp_plan_runs (id) ON DELETE CASCADE
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mrp_action_messages (
    id uuid NOT NULL,
    plan_run_id uuid NOT NULL,
    product_id uuid NOT NULL,
    action_type character varying(30) NOT NULL,
    severity character varying(20) NOT NULL,
    quantity numeric(18,4) NOT NULL,
    current_date_utc timestamp with time zone NULL,
    suggested_date_utc timestamp with time zone NULL,
    related_purchase_order_id uuid NULL,
    related_planned_order_id uuid NULL,
    days_until_stock_out integer NOT NULL,
    message character varying(500) NOT NULL,
    is_dismissed boolean NOT NULL,
    dismissed_by_user_id uuid NULL,
    dismissed_at_utc timestamp with time zone NULL,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_mrp_action_messages PRIMARY KEY (id),
    CONSTRAINT fk_mrp_action_messages_mrp_plan_runs_plan_run_id FOREIGN KEY (plan_run_id) REFERENCES mrp_plan_runs (id) ON DELETE CASCADE
);
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS mrp_peggings (
    id uuid NOT NULL,
    plan_run_id uuid NOT NULL,
    component_product_id uuid NOT NULL,
    requirement_quantity numeric(18,4) NOT NULL,
    due_date_utc timestamp with time zone NOT NULL,
    source_kind character varying(30) NOT NULL,
    source_parent_product_id uuid NULL,
    source_order_line_id uuid NULL,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_mrp_peggings PRIMARY KEY (id),
    CONSTRAINT fk_mrp_peggings_mrp_plan_runs_plan_run_id FOREIGN KEY (plan_run_id) REFERENCES mrp_plan_runs (id) ON DELETE CASCADE
);
");

            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX IF NOT EXISTS ix_mrp_plan_runs_tenant_idempotency_unique ON mrp_plan_runs (tenant_id, idempotency_key);
CREATE INDEX IF NOT EXISTS ix_mrp_plan_runs_tenant_id_as_of_date_utc ON mrp_plan_runs (tenant_id, as_of_date_utc);
CREATE INDEX IF NOT EXISTS ix_mrp_planned_orders_tenant_id_plan_run_id ON mrp_planned_orders (tenant_id, plan_run_id);
CREATE INDEX IF NOT EXISTS ix_mrp_planned_orders_tenant_id_product_id ON mrp_planned_orders (tenant_id, product_id);
CREATE INDEX IF NOT EXISTS ix_mrp_action_messages_tenant_id_plan_run_id_action_type ON mrp_action_messages (tenant_id, plan_run_id, action_type);
CREATE INDEX IF NOT EXISTS ix_mrp_action_messages_tenant_id_is_dismissed ON mrp_action_messages (tenant_id, is_dismissed);
CREATE INDEX IF NOT EXISTS ix_mrp_peggings_tenant_id_plan_run_id_component_product_id ON mrp_peggings (tenant_id, plan_run_id, component_product_id);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS mrp_peggings;
DROP TABLE IF EXISTS mrp_action_messages;
DROP TABLE IF EXISTS mrp_planned_orders;
DROP TABLE IF EXISTS mrp_plan_runs;
");

            migrationBuilder.Sql(@"
ALTER TABLE products DROP COLUMN IF EXISTS service_level_target;
ALTER TABLE products DROP COLUMN IF EXISTS holding_cost_rate;
ALTER TABLE products DROP COLUMN IF EXISTS ordering_cost;
ALTER TABLE products DROP COLUMN IF EXISTS eoq_annual_demand;
ALTER TABLE products DROP COLUMN IF EXISTS order_multiple;
ALTER TABLE products DROP COLUMN IF EXISTS fixed_order_quantity;
ALTER TABLE products DROP COLUMN IF EXISTS lot_sizing_policy;
");
        }
    }
}
