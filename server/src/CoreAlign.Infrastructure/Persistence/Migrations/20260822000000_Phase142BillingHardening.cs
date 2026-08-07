using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase142BillingHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE tenant_modules ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;
ALTER TABLE subscription_orders ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;
ALTER TABLE subscription_orders ADD COLUMN IF NOT EXISTS gateway_redirect_url character varying(1000);
ALTER TABLE subscription_orders ADD COLUMN IF NOT EXISTS operation_id uuid;
");

            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ix_subscription_orders_gateway_intent;
CREATE UNIQUE INDEX IF NOT EXISTS ix_subscription_orders_gateway_intent
    ON subscription_orders (gateway_name, gateway_intent_id)
    WHERE gateway_intent_id IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_subscription_orders_tenant_operation
    ON subscription_orders (tenant_id, operation_id)
    WHERE operation_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_tenant_modules_module_id ON tenant_modules (module_id);
");

            // NOT VALID skips the scan of existing rows (there are none orphaned today, but a
            // restored dump must not block the deploy) while still enforcing every new row.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_tenant_modules_modules_module_id'
          AND conrelid = 'tenant_modules'::regclass)
    THEN
        ALTER TABLE tenant_modules
            ADD CONSTRAINT fk_tenant_modules_modules_module_id
            FOREIGN KEY (module_id) REFERENCES modules (id) ON DELETE RESTRICT NOT VALID;
    END IF;

    -- Promote to fully validated when the data already satisfies it; a dump carrying orphaned
    -- grants keeps the constraint unvalidated (still enforced for new rows) instead of failing.
    IF NOT EXISTS (
        SELECT 1 FROM tenant_modules tm
        LEFT JOIN modules m ON m.id = tm.module_id
        WHERE m.id IS NULL)
    THEN
        ALTER TABLE tenant_modules VALIDATE CONSTRAINT fk_tenant_modules_modules_module_id;
    END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE tenant_modules DROP CONSTRAINT IF EXISTS fk_tenant_modules_modules_module_id;
DROP INDEX IF EXISTS ix_tenant_modules_module_id;
DROP INDEX IF EXISTS ux_subscription_orders_tenant_operation;
DROP INDEX IF EXISTS ix_subscription_orders_gateway_intent;
CREATE INDEX IF NOT EXISTS ix_subscription_orders_gateway_intent
    ON subscription_orders (gateway_name, gateway_intent_id)
    WHERE gateway_intent_id IS NOT NULL;
ALTER TABLE subscription_orders DROP COLUMN IF EXISTS operation_id;
ALTER TABLE subscription_orders DROP COLUMN IF EXISTS gateway_redirect_url;
ALTER TABLE subscription_orders DROP COLUMN IF EXISTS concurrency_token;
ALTER TABLE tenant_modules DROP COLUMN IF EXISTS concurrency_token;
");
        }
    }
}
