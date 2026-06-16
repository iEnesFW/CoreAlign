using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase88ScaleSortIndexes : Migration
    {
        // Sort/keyset/composite indexes so list queries stay index-backed (top-N + keyset)
        // at millions of rows. Raw SQL (additive, IF NOT EXISTS) — EF does not manage these,
        // so no snapshot recreate. tenant_id-leading + trailing id/sort tiebreaker for keyset.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- missing sort indexes (no covering index existed)
CREATE INDEX IF NOT EXISTS ix_shipments_tenant_id_created_date ON shipments (tenant_id, created_date DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_entity_audit_logs_tenant_id_changed_at ON entity_audit_logs (tenant_id, changed_at_utc DESC, sequence DESC);
CREATE INDEX IF NOT EXISTS ix_customers_tenant_id_created_at_utc ON customers (tenant_id, created_at_utc DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_products_tenant_id_created_at_utc ON products (tenant_id, created_at_utc DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_vendors_tenant_id_created_at_utc ON vendors (tenant_id, created_at_utc DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_provider_webhook_inbox_tenant_id_received_at ON provider_webhook_inbox (tenant_id, received_at_utc DESC);
CREATE INDEX IF NOT EXISTS ix_notification_messages_tenant_user_created ON notification_messages (tenant_id, user_id, created_at_utc DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_notification_messages_tenant_user_unread ON notification_messages (tenant_id, user_id, created_at_utc DESC, id DESC) WHERE status <> 'Read';

-- core ERP document lists: tenant-leading + sort-date + id tiebreaker (keyset-ready)
CREATE INDEX IF NOT EXISTS ix_orders_tenant_dealer_orderdate ON orders (tenant_id, origin_dealer_account_id, order_date DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_orders_tenant_orderdate_id ON orders (tenant_id, order_date DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_invoices_tenant_issuedate_id ON invoices (tenant_id, issue_date DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_payments_tenant_paydate_id ON payments (tenant_id, payment_date DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_journal_entries_tenant_posting_number ON journal_entries (tenant_id, posting_date DESC, number DESC);
CREATE INDEX IF NOT EXISTS ix_quotes_tenant_quotedate_id ON quotes (tenant_id, quote_date DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_vendor_bills_tenant_billdate_id ON vendor_bills (tenant_id, bill_date DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_vendor_payments_tenant_paydate_id ON vendor_payments (tenant_id, payment_date DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_purchase_orders_tenant_orderdate_id ON purchase_orders (tenant_id, order_date DESC, id DESC);

-- append-only ledger/transaction tables (partitioned): keyset composite with trailing id
CREATE INDEX IF NOT EXISTS ix_customer_transactions_keyset ON customer_transactions (tenant_id, customer_id, occurred_at_utc DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_stock_transactions_keyset ON stock_transactions (tenant_id, product_id, occurred_at_utc DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_stock_movements_keyset ON stock_movements (tenant_id, occurred_at_utc DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_customer_ledger_keyset ON customer_ledger_entries (tenant_id, customer_id, occurred_at_utc DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_vendor_ledger_keyset ON vendor_ledger_entries (tenant_id, vendor_id, occurred_at_utc DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_dealer_commission_keyset ON dealer_commission_ledger_entries (tenant_id, dealer_account_id, accrued_at_utc DESC, id DESC);

-- dashboard low-stock partial covering + open-AR filter
CREATE INDEX IF NOT EXISTS ix_products_tenant_lowstock ON products (tenant_id, stock_quantity) WHERE status IN ('Active','New');
CREATE INDEX IF NOT EXISTS ix_invoices_tenant_status_due ON invoices (tenant_id, status, due_date);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ix_shipments_tenant_id_created_date;
DROP INDEX IF EXISTS ix_entity_audit_logs_tenant_id_changed_at;
DROP INDEX IF EXISTS ix_customers_tenant_id_created_at_utc;
DROP INDEX IF EXISTS ix_products_tenant_id_created_at_utc;
DROP INDEX IF EXISTS ix_vendors_tenant_id_created_at_utc;
DROP INDEX IF EXISTS ix_provider_webhook_inbox_tenant_id_received_at;
DROP INDEX IF EXISTS ix_notification_messages_tenant_user_created;
DROP INDEX IF EXISTS ix_notification_messages_tenant_user_unread;
DROP INDEX IF EXISTS ix_orders_tenant_dealer_orderdate;
DROP INDEX IF EXISTS ix_orders_tenant_orderdate_id;
DROP INDEX IF EXISTS ix_invoices_tenant_issuedate_id;
DROP INDEX IF EXISTS ix_payments_tenant_paydate_id;
DROP INDEX IF EXISTS ix_journal_entries_tenant_posting_number;
DROP INDEX IF EXISTS ix_quotes_tenant_quotedate_id;
DROP INDEX IF EXISTS ix_vendor_bills_tenant_billdate_id;
DROP INDEX IF EXISTS ix_vendor_payments_tenant_paydate_id;
DROP INDEX IF EXISTS ix_purchase_orders_tenant_orderdate_id;
DROP INDEX IF EXISTS ix_customer_transactions_keyset;
DROP INDEX IF EXISTS ix_stock_transactions_keyset;
DROP INDEX IF EXISTS ix_stock_movements_keyset;
DROP INDEX IF EXISTS ix_customer_ledger_keyset;
DROP INDEX IF EXISTS ix_vendor_ledger_keyset;
DROP INDEX IF EXISTS ix_dealer_commission_keyset;
DROP INDEX IF EXISTS ix_products_tenant_lowstock;
DROP INDEX IF EXISTS ix_invoices_tenant_status_due;");
        }
    }
}
