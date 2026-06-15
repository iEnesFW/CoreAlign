using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phase 5.2 hardening — adds a client-supplied idempotency key to
    /// <c>payment_transactions</c> with a partial unique index on
    /// <c>(tenant_id, idempotency_key)</c>. The key is supplied by the caller
    /// of <c>ChargePaymentCommand</c> / <c>Initiate3DSecureCommand</c> and is
    /// also forwarded to providers (Stripe Idempotency-Key header) so retries
    /// on the same logical attempt collapse into a single transaction.
    /// </summary>
    public partial class Phase52PaymentIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "payment_transactions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_payment_transactions_tenant_idempotency_key",
                table: "payment_transactions",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true,
                filter: "\"idempotency_key\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_payment_transactions_tenant_idempotency_key",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "payment_transactions");
        }
    }
}
