using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase141FiscalYearStartMonth : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The column shipped with a DB default of 0 — a month that does not exist. EF always sends
        // the entity initializer (1), so the default only bites a row written outside EF (a raw
        // insert, a restore, a fixture), and then every fiscal-year window computed from it would
        // be nonsense. Heal existing rows, then make the storage layer agree with the domain.
        migrationBuilder.Sql(@"
UPDATE tenants SET fiscal_year_start_month = 1
WHERE fiscal_year_start_month IS NULL OR fiscal_year_start_month < 1 OR fiscal_year_start_month > 12;");

        migrationBuilder.Sql(
            "ALTER TABLE tenants ALTER COLUMN fiscal_year_start_month SET DEFAULT 1;");

        migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_tenants_fiscal_year_start_month') THEN
        ALTER TABLE tenants ADD CONSTRAINT ck_tenants_fiscal_year_start_month
            CHECK (fiscal_year_start_month BETWEEN 1 AND 12);
    END IF;
END $$;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "ALTER TABLE tenants DROP CONSTRAINT IF EXISTS ck_tenants_fiscal_year_start_month;");
        migrationBuilder.Sql(
            "ALTER TABLE tenants ALTER COLUMN fiscal_year_start_month SET DEFAULT 0;");
    }
}
