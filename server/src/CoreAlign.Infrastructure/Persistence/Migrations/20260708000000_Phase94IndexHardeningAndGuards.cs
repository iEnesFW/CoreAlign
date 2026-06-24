using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase94IndexHardeningAndGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX IF NOT EXISTS ux_payment_applications_tenant_payment_invoice
    ON payment_applications (tenant_id, payment_id, invoice_id);
DROP INDEX IF EXISTS ix_payment_applications_tenant_id_payment_id;

CREATE UNIQUE INDEX IF NOT EXISTS ux_vendor_payment_applications_tenant_payment_bill
    ON vendor_payment_applications (tenant_id, vendor_payment_id, vendor_bill_id);
DROP INDEX IF EXISTS ix_vendor_payment_applications_tenant_id_vendor_payment_id;

CREATE INDEX IF NOT EXISTS ix_orders_tenant_id_status_order_date
    ON orders (tenant_id, status, order_date DESC);
DROP INDEX IF EXISTS ix_orders_tenant_id_status;

CREATE INDEX IF NOT EXISTS ix_shipments_tenant_id_status_created_date
    ON shipments (tenant_id, status, created_date DESC);
DROP INDEX IF EXISTS ix_shipments_tenant_id_status;

CREATE INDEX IF NOT EXISTS ix_outbox_messages_next_attempt_utc
    ON outbox_messages (next_attempt_utc) WHERE status IN ('Pending', 'Deferred');
DROP INDEX IF EXISTS ix_outbox_messages_status_next_attempt_utc;

DROP INDEX IF EXISTS ix_stock_items_tenant_id_product_id;
DROP INDEX IF EXISTS ix_stock_movements_tenant_id_occurred_at_utc;
DROP INDEX IF EXISTS ix_stock_transactions_tenant_id_product_id_occurred_at_utc;
DROP INDEX IF EXISTS ix_vendor_ledger_entries_tenant_id_vendor_id_occurred_at_utc;
DROP INDEX IF EXISTS ix_quotes_tenant_id_quote_date;
DROP INDEX IF EXISTS ix_purchase_orders_tenant_id_order_date;
DROP INDEX IF EXISTS ix_vendor_bills_tenant_id_bill_date;");

            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ux_retention_policies_tenant_entity;
CREATE UNIQUE INDEX IF NOT EXISTS ux_retention_policies_tenant_entity
    ON retention_policies (tenant_id, entity_type) WHERE is_deleted = false;

DROP INDEX IF EXISTS ux_tenant_identity_providers_tenant_name;
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_identity_providers_tenant_name
    ON tenant_identity_providers (tenant_id, name) WHERE is_deleted = false;

DROP INDEX IF EXISTS ux_notification_templates_tenant_key_channel_locale;
CREATE UNIQUE INDEX IF NOT EXISTS ux_notification_templates_tenant_key_channel_locale
    ON notification_templates (tenant_id, key, channel, locale) WHERE is_deleted = false;

DROP INDEX IF EXISTS ux_installation_acceptances_tenant_workorder;
CREATE UNIQUE INDEX IF NOT EXISTS ux_installation_acceptances_tenant_workorder
    ON installation_acceptances (tenant_id, work_order_id) WHERE is_deleted = false;");

            migrationBuilder.Sql(@"
DO $fk$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_customer_transactions_customers_customer_id') THEN
        ALTER TABLE customer_transactions ADD CONSTRAINT fk_customer_transactions_customers_customer_id
            FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_customer_transactions_invoices_invoice_id') THEN
        ALTER TABLE customer_transactions ADD CONSTRAINT fk_customer_transactions_invoices_invoice_id
            FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE SET NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_customer_transactions_orders_order_id') THEN
        ALTER TABLE customer_transactions ADD CONSTRAINT fk_customer_transactions_orders_order_id
            FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE SET NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_stock_movements_products_product_id') THEN
        ALTER TABLE stock_movements ADD CONSTRAINT fk_stock_movements_products_product_id
            FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_stock_movements_warehouses_warehouse_id') THEN
        ALTER TABLE stock_movements ADD CONSTRAINT fk_stock_movements_warehouses_warehouse_id
            FOREIGN KEY (warehouse_id) REFERENCES warehouses(id) ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_stock_movements_lots_lot_id') THEN
        ALTER TABLE stock_movements ADD CONSTRAINT fk_stock_movements_lots_lot_id
            FOREIGN KEY (lot_id) REFERENCES lots(id) ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_stock_movements_stock_reason_codes_reason_code_id') THEN
        ALTER TABLE stock_movements ADD CONSTRAINT fk_stock_movements_stock_reason_codes_reason_code_id
            FOREIGN KEY (reason_code_id) REFERENCES stock_reason_codes(id) ON DELETE RESTRICT;
    END IF;
END
$fk$;");

            migrationBuilder.Sql(@"
DO $outer$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'corealign_app') THEN
        CREATE ROLE corealign_app LOGIN;
    END IF;
END
$outer$;

GRANT USAGE ON SCHEMA public TO corealign_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO corealign_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO corealign_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO corealign_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO corealign_app;

DO $outer$
DECLARE t text;
BEGIN
    FOR t IN
        SELECT DISTINCT cl.relname
        FROM pg_constraint con
        JOIN pg_class cl ON cl.oid = con.conrelid
        JOIN pg_class ref ON ref.oid = con.confrelid
        JOIN pg_namespace ns ON ns.oid = cl.relnamespace
        WHERE con.contype = 'f'
          AND ref.relname = 'tenants'
          AND ns.nspname = 'public'
          AND cl.relname <> 'users'
    LOOP
        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', t);
        EXECUTE format('ALTER TABLE public.%I FORCE ROW LEVEL SECURITY', t);
        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON public.%I', t);
        EXECUTE format(
            $pol$CREATE POLICY tenant_isolation ON public.%I
                 USING (tenant_id = current_setting('app.tenant_id', true)::uuid
                        OR current_setting('app.rls_bypass', true) = '1')
                 WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid
                        OR current_setting('app.rls_bypass', true) = '1')$pol$, t);
    END LOOP;
END
$outer$;");

            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS two_factor_backup_code;
DROP TABLE IF EXISTS two_factor_challenge;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ux_installation_acceptances_tenant_workorder;
CREATE UNIQUE INDEX IF NOT EXISTS ux_installation_acceptances_tenant_workorder
    ON installation_acceptances (tenant_id, work_order_id);

DROP INDEX IF EXISTS ux_notification_templates_tenant_key_channel_locale;
CREATE UNIQUE INDEX IF NOT EXISTS ux_notification_templates_tenant_key_channel_locale
    ON notification_templates (tenant_id, key, channel, locale);

DROP INDEX IF EXISTS ux_tenant_identity_providers_tenant_name;
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_identity_providers_tenant_name
    ON tenant_identity_providers (tenant_id, name);

DROP INDEX IF EXISTS ux_retention_policies_tenant_entity;
CREATE UNIQUE INDEX IF NOT EXISTS ux_retention_policies_tenant_entity
    ON retention_policies (tenant_id, entity_type);

CREATE INDEX IF NOT EXISTS ix_vendor_bills_tenant_id_bill_date ON vendor_bills (tenant_id, bill_date DESC);
CREATE INDEX IF NOT EXISTS ix_purchase_orders_tenant_id_order_date ON purchase_orders (tenant_id, order_date DESC);
CREATE INDEX IF NOT EXISTS ix_quotes_tenant_id_quote_date ON quotes (tenant_id, quote_date DESC);
CREATE INDEX IF NOT EXISTS ix_vendor_ledger_entries_tenant_id_vendor_id_occurred_at_utc ON vendor_ledger_entries (tenant_id, vendor_id, occurred_at_utc);
CREATE INDEX IF NOT EXISTS ix_stock_transactions_tenant_id_product_id_occurred_at_utc ON stock_transactions (tenant_id, product_id, occurred_at_utc DESC);
CREATE INDEX IF NOT EXISTS ix_stock_movements_tenant_id_occurred_at_utc ON stock_movements (tenant_id, occurred_at_utc DESC);
CREATE INDEX IF NOT EXISTS ix_stock_items_tenant_id_product_id ON stock_items (tenant_id, product_id);

CREATE INDEX IF NOT EXISTS ix_outbox_messages_status_next_attempt_utc ON outbox_messages (status, next_attempt_utc);
DROP INDEX IF EXISTS ix_outbox_messages_next_attempt_utc;

CREATE INDEX IF NOT EXISTS ix_shipments_tenant_id_status ON shipments (tenant_id, status);
DROP INDEX IF EXISTS ix_shipments_tenant_id_status_created_date;

CREATE INDEX IF NOT EXISTS ix_orders_tenant_id_status ON orders (tenant_id, status);
DROP INDEX IF EXISTS ix_orders_tenant_id_status_order_date;

CREATE INDEX IF NOT EXISTS ix_vendor_payment_applications_tenant_id_vendor_payment_id ON vendor_payment_applications (tenant_id, vendor_payment_id);
DROP INDEX IF EXISTS ux_vendor_payment_applications_tenant_payment_bill;

CREATE INDEX IF NOT EXISTS ix_payment_applications_tenant_id_payment_id ON payment_applications (tenant_id, payment_id);
DROP INDEX IF EXISTS ux_payment_applications_tenant_payment_invoice;");
        }
    }
}
