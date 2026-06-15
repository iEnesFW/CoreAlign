using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase78FinancialCheckConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_journal_lines_debit_nonneg') THEN
    ALTER TABLE journal_lines ADD CONSTRAINT ck_journal_lines_debit_nonneg CHECK (debit >= 0);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_journal_lines_credit_nonneg') THEN
    ALTER TABLE journal_lines ADD CONSTRAINT ck_journal_lines_credit_nonneg CHECK (credit >= 0);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_journal_lines_debit_xor_credit') THEN
    ALTER TABLE journal_lines ADD CONSTRAINT ck_journal_lines_debit_xor_credit CHECK (NOT (debit > 0 AND credit > 0));
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_journal_entries_total_debit_nonneg') THEN
    ALTER TABLE journal_entries ADD CONSTRAINT ck_journal_entries_total_debit_nonneg CHECK (total_debit >= 0);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_journal_entries_total_credit_nonneg') THEN
    ALTER TABLE journal_entries ADD CONSTRAINT ck_journal_entries_total_credit_nonneg CHECK (total_credit >= 0);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_customer_product_prices_discount_pct') THEN
    ALTER TABLE customer_product_prices ADD CONSTRAINT ck_customer_product_prices_discount_pct CHECK (discount_percent >= 0 AND discount_percent <= 100);
  END IF;
END $$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE journal_lines DROP CONSTRAINT IF EXISTS ck_journal_lines_debit_nonneg;
ALTER TABLE journal_lines DROP CONSTRAINT IF EXISTS ck_journal_lines_credit_nonneg;
ALTER TABLE journal_lines DROP CONSTRAINT IF EXISTS ck_journal_lines_debit_xor_credit;
ALTER TABLE journal_entries DROP CONSTRAINT IF EXISTS ck_journal_entries_total_debit_nonneg;
ALTER TABLE journal_entries DROP CONSTRAINT IF EXISTS ck_journal_entries_total_credit_nonneg;
ALTER TABLE customer_product_prices DROP CONSTRAINT IF EXISTS ck_customer_product_prices_discount_pct;");
        }
    }
}
