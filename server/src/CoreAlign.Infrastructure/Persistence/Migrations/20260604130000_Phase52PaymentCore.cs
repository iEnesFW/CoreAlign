using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phase 5.2 — Payment provider core (F2.2). Adds the
    /// <c>payment_transactions</c> ledger consumed by <c>PaymentDispatcher</c>
    /// and the reconciliation job. The provider registry / webhook inbox
    /// tables were already provisioned by Phase 4.2.
    /// </summary>
    public partial class Phase52PaymentCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    external_transaction_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    attempted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    requires_three_d_secure = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    redirect_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    refunded_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    metadata_json = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_transactions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_tenant_provider_external",
                table: "payment_transactions",
                columns: new[] { "tenant_id", "provider_name", "external_transaction_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_tenant_status",
                table: "payment_transactions",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_tenant_orderref",
                table: "payment_transactions",
                columns: new[] { "tenant_id", "order_reference" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "payment_transactions");
        }
    }
}
