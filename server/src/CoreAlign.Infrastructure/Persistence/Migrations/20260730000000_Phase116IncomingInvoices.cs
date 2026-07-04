using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase116IncomingInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS incoming_invoices (
                    id uuid NOT NULL,
                    ettn character varying(64) NOT NULL,
                    sender_vkn character varying(16) NOT NULL,
                    sender_name character varying(300),
                    invoice_number character varying(64) NOT NULL,
                    issue_date timestamp with time zone NOT NULL,
                    provider_name character varying(64) NOT NULL,
                    provider_status character varying(40),
                    status integer NOT NULL,
                    linked_vendor_bill_id uuid,
                    processed_at_utc timestamp with time zone,
                    notes character varying(1000),
                    tenant_id uuid NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT pk_incoming_invoices PRIMARY KEY (id),
                    CONSTRAINT fk_incoming_invoices_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
                );
                """);
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ix_incoming_invoices_tenant_id_ettn ON incoming_invoices (tenant_id, ettn);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_incoming_invoices_tenant_id_status_issue_date ON incoming_invoices (tenant_id, status, issue_date DESC);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS incoming_invoices;");
        }
    }
}
