using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase107BankAccountMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS bank_accounts (
                    id uuid NOT NULL,
                    account_name character varying(200) NOT NULL,
                    bank_name character varying(200) NOT NULL,
                    branch_name character varying(100),
                    iban character varying(34) NOT NULL,
                    swift character varying(11),
                    currency character varying(3) NOT NULL,
                    opening_balance numeric(18,4) NOT NULL,
                    is_primary boolean NOT NULL,
                    is_active boolean NOT NULL,
                    notes character varying(1000),
                    tenant_id uuid NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT pk_bank_accounts PRIMARY KEY (id),
                    CONSTRAINT fk_bank_accounts_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
                );");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ix_bank_accounts_tenant_id_iban ON bank_accounts (tenant_id, iban);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_bank_accounts_tenant_id_is_active ON bank_accounts (tenant_id, is_active);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS bank_accounts;");
        }
    }
}
