using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase119FinanceConcurrencyTokens : Migration
    {
        // App-managed optimistic concurrency token (long) for the financial aggregates that
        // previously relied on the xmin system column — disabled as a no-op on PostgreSQL 18
        // (see CoreAlignDbContext.ApplyXminConcurrencyTokens). Mirrors the Phase117
        // (VendorBill/VendorPayment/PurchaseOrder) idempotent ADD COLUMN pattern.
        private static readonly string[] Tables =
        {
            "invoices",
            "orders",
            "payments",
            "journal_entries",
            "customer_ledger_entries",
            "vendor_ledger_entries",
            "employees",
            "payroll_runs",
            "payslips",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql(
                    $"ALTER TABLE {table} ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql(
                    $"ALTER TABLE {table} DROP COLUMN IF EXISTS concurrency_token;");
            }
        }
    }
}
