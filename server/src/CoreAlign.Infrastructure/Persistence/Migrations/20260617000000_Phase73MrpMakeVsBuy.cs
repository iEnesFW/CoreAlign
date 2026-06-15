using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// MRP make-vs-buy routing (Tranche T2, Group B). Idempotent: re-applying is a
    /// no-op via IF NOT EXISTS. Adds products.procurement_type (Buy default, stored as
    /// string to match the EF HasConversion&lt;string&gt; mapping, mirroring products.status)
    /// and the planned_production_orders table — the planned-order sink for Make items,
    /// distinct from the parallel-agent glass work orders. The DEFAULT 'Buy' backfills
    /// existing rows with a valid enum literal (an empty string would fail enum parsing).
    /// </summary>
    public partial class Phase73MrpMakeVsBuy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE products ADD COLUMN IF NOT EXISTS procurement_type character varying(10) NOT NULL DEFAULT 'Buy';
");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS planned_production_orders (
    id uuid NOT NULL,
    source_plan_run_id uuid NOT NULL,
    product_id uuid NOT NULL,
    low_level_code integer NOT NULL,
    quantity numeric(18,4) NOT NULL,
    due_date_utc timestamp with time zone NOT NULL,
    release_date_utc timestamp with time zone NOT NULL,
    estimated_unit_cost numeric(18,4) NOT NULL,
    source_policy character varying(30) NOT NULL,
    pegging_parent_product_id uuid NULL,
    pegging_source_order_line_id uuid NULL,
    status character varying(20) NOT NULL,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_planned_production_orders PRIMARY KEY (id)
);
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS ix_planned_production_orders_tenant_id_source_plan_run_id ON planned_production_orders (tenant_id, source_plan_run_id);
CREATE INDEX IF NOT EXISTS ix_planned_production_orders_tenant_id_product_id ON planned_production_orders (tenant_id, product_id);
CREATE INDEX IF NOT EXISTS ix_planned_production_orders_tenant_run_pegging_order_line ON planned_production_orders (tenant_id, source_plan_run_id, pegging_source_order_line_id);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS planned_production_orders;
");

            migrationBuilder.Sql(@"
ALTER TABLE products DROP COLUMN IF EXISTS procurement_type;
");
        }
    }
}
