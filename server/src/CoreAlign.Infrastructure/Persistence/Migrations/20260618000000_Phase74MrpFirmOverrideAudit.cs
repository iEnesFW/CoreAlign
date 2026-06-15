using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// MRP firm/planner-override audit columns + FK-index backfill (Tranche T3). Idempotent.
    ///
    /// Adds original_quantity + original_due_date_utc to mrp_planned_orders and
    /// planned_production_orders, capturing the pre-override values when a planner firms with a
    /// quantity/date override (so the override is visible and reversible).
    ///
    /// Also backfills the three single-column FK indexes the EF model derives from the
    /// MrpPlanRun→children relationships (ix_mrp_*_plan_run_id) but the hand-authored Phase72
    /// SQL never created (it created only the composite (tenant_id, plan_run_id) indexes). Without
    /// this the snapshot/model claim indexes the DB lacks — a silent model-vs-DB drift.
    ///
    /// NOTE on table names: the Phase72 aggregates had no explicit EF table mapping, so the model
    /// defaulted to SINGULAR names while Phase72 SQL created PLURAL tables. The fix is the
    /// ToTable("&lt;plural&gt;") added to the 4 MRP configs — a MODEL-side correction only. Real Postgres
    /// has always been plural (Phase72 SQL), so NO rename DDL is needed here and none is emitted.
    /// </summary>
    public partial class Phase74MrpFirmOverrideAudit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE mrp_planned_orders ADD COLUMN IF NOT EXISTS original_quantity numeric(18,4) NULL;
ALTER TABLE mrp_planned_orders ADD COLUMN IF NOT EXISTS original_due_date_utc timestamp with time zone NULL;
ALTER TABLE planned_production_orders ADD COLUMN IF NOT EXISTS original_quantity numeric(18,4) NULL;
ALTER TABLE planned_production_orders ADD COLUMN IF NOT EXISTS original_due_date_utc timestamp with time zone NULL;
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS ix_mrp_planned_orders_plan_run_id ON mrp_planned_orders (plan_run_id);
CREATE INDEX IF NOT EXISTS ix_mrp_action_messages_plan_run_id ON mrp_action_messages (plan_run_id);
CREATE INDEX IF NOT EXISTS ix_mrp_peggings_plan_run_id ON mrp_peggings (plan_run_id);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ix_mrp_planned_orders_plan_run_id;
DROP INDEX IF EXISTS ix_mrp_action_messages_plan_run_id;
DROP INDEX IF EXISTS ix_mrp_peggings_plan_run_id;
");

            migrationBuilder.Sql(@"
ALTER TABLE mrp_planned_orders DROP COLUMN IF EXISTS original_quantity;
ALTER TABLE mrp_planned_orders DROP COLUMN IF EXISTS original_due_date_utc;
ALTER TABLE planned_production_orders DROP COLUMN IF EXISTS original_quantity;
ALTER TABLE planned_production_orders DROP COLUMN IF EXISTS original_due_date_utc;
");
        }
    }
}
