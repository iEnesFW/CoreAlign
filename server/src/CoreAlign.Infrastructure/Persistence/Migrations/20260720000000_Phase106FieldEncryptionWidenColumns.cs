using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase106FieldEncryptionWidenColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_vendor_bank_accounts_tenant_id_iban;");
            migrationBuilder.Sql("ALTER TABLE vendor_bank_accounts ALTER COLUMN swift TYPE text;");
            migrationBuilder.Sql("ALTER TABLE vendor_bank_accounts ALTER COLUMN iban TYPE text;");
            migrationBuilder.Sql("ALTER TABLE vendor_bank_accounts ALTER COLUMN account_number TYPE text;");
            migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN two_factor_secret_key TYPE text;");
            migrationBuilder.Sql("ALTER TABLE payslips ALTER COLUMN national_id TYPE text;");
            migrationBuilder.Sql("ALTER TABLE employees ALTER COLUMN sgk_registration_no TYPE text;");
            migrationBuilder.Sql("ALTER TABLE employees ALTER COLUMN iban TYPE text;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE employees ALTER COLUMN iban TYPE character varying(34);");
            migrationBuilder.Sql("ALTER TABLE employees ALTER COLUMN sgk_registration_no TYPE character varying(32);");
            migrationBuilder.Sql("ALTER TABLE payslips ALTER COLUMN national_id TYPE char(11);");
            migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN two_factor_secret_key TYPE character varying(256);");
            migrationBuilder.Sql("ALTER TABLE vendor_bank_accounts ALTER COLUMN account_number TYPE character varying(64);");
            migrationBuilder.Sql("ALTER TABLE vendor_bank_accounts ALTER COLUMN iban TYPE character varying(34);");
            migrationBuilder.Sql("ALTER TABLE vendor_bank_accounts ALTER COLUMN swift TYPE character varying(11);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_vendor_bank_accounts_tenant_id_iban ON vendor_bank_accounts (tenant_id, iban);");
        }
    }
}
