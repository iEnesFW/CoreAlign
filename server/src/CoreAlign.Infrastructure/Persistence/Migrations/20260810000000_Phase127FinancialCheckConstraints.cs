using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase127FinancialCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_journal_entries_balanced') THEN
    ALTER TABLE journal_entries ADD CONSTRAINT ck_journal_entries_balanced CHECK (status <> 'Posted' OR total_debit = total_credit);
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_invoice_lines_line_discount_pct') THEN
    ALTER TABLE invoice_lines ADD CONSTRAINT ck_invoice_lines_line_discount_pct CHECK (line_discount_percent >= 0 AND line_discount_percent <= 100);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_invoice_lines_tax_rate_pct') THEN
    ALTER TABLE invoice_lines ADD CONSTRAINT ck_invoice_lines_tax_rate_pct CHECK (tax_rate_percent >= 0 AND tax_rate_percent <= 100);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_invoice_lines_withholding_pct') THEN
    ALTER TABLE invoice_lines ADD CONSTRAINT ck_invoice_lines_withholding_pct CHECK (withholding_rate_percent >= 0 AND withholding_rate_percent <= 100);
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_order_lines_line_discount_pct') THEN
    ALTER TABLE order_lines ADD CONSTRAINT ck_order_lines_line_discount_pct CHECK (line_discount_percent >= 0 AND line_discount_percent <= 100);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_order_lines_tax_rate_pct') THEN
    ALTER TABLE order_lines ADD CONSTRAINT ck_order_lines_tax_rate_pct CHECK (tax_rate_percent >= 0 AND tax_rate_percent <= 100);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_order_lines_withholding_pct') THEN
    ALTER TABLE order_lines ADD CONSTRAINT ck_order_lines_withholding_pct CHECK (withholding_rate_percent >= 0 AND withholding_rate_percent <= 100);
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_quote_lines_line_discount_pct') THEN
    ALTER TABLE quote_lines ADD CONSTRAINT ck_quote_lines_line_discount_pct CHECK (line_discount_percent >= 0 AND line_discount_percent <= 100);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_quote_lines_tax_rate_pct') THEN
    ALTER TABLE quote_lines ADD CONSTRAINT ck_quote_lines_tax_rate_pct CHECK (tax_rate_percent >= 0 AND tax_rate_percent <= 100);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_quote_lines_withholding_pct') THEN
    ALTER TABLE quote_lines ADD CONSTRAINT ck_quote_lines_withholding_pct CHECK (withholding_rate_percent >= 0 AND withholding_rate_percent <= 100);
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_purchase_order_lines_tax_rate_pct') THEN
    ALTER TABLE purchase_order_lines ADD CONSTRAINT ck_purchase_order_lines_tax_rate_pct CHECK (tax_rate_percent >= 0 AND tax_rate_percent <= 100);
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_vendor_bill_lines_tax_rate_pct') THEN
    ALTER TABLE vendor_bill_lines ADD CONSTRAINT ck_vendor_bill_lines_tax_rate_pct CHECK (tax_rate_percent >= 0 AND tax_rate_percent <= 100);
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_tax_rates_rate_pct') THEN
    ALTER TABLE tax_rates ADD CONSTRAINT ck_tax_rates_rate_pct CHECK (rate_percent >= 0 AND rate_percent <= 100);
  END IF;
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE journal_entries DROP CONSTRAINT IF EXISTS ck_journal_entries_balanced;
ALTER TABLE invoice_lines DROP CONSTRAINT IF EXISTS ck_invoice_lines_line_discount_pct;
ALTER TABLE invoice_lines DROP CONSTRAINT IF EXISTS ck_invoice_lines_tax_rate_pct;
ALTER TABLE invoice_lines DROP CONSTRAINT IF EXISTS ck_invoice_lines_withholding_pct;
ALTER TABLE order_lines DROP CONSTRAINT IF EXISTS ck_order_lines_line_discount_pct;
ALTER TABLE order_lines DROP CONSTRAINT IF EXISTS ck_order_lines_tax_rate_pct;
ALTER TABLE order_lines DROP CONSTRAINT IF EXISTS ck_order_lines_withholding_pct;
ALTER TABLE quote_lines DROP CONSTRAINT IF EXISTS ck_quote_lines_line_discount_pct;
ALTER TABLE quote_lines DROP CONSTRAINT IF EXISTS ck_quote_lines_tax_rate_pct;
ALTER TABLE quote_lines DROP CONSTRAINT IF EXISTS ck_quote_lines_withholding_pct;
ALTER TABLE purchase_order_lines DROP CONSTRAINT IF EXISTS ck_purchase_order_lines_tax_rate_pct;
ALTER TABLE vendor_bill_lines DROP CONSTRAINT IF EXISTS ck_vendor_bill_lines_tax_rate_pct;
ALTER TABLE tax_rates DROP CONSTRAINT IF EXISTS ck_tax_rates_rate_pct;");
        }
    }
}
