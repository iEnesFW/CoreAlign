using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase120ShipmentConcurrencyToken : Migration
    {
        // App-managed optimistic-concurrency token for Shipment so concurrent e-Despatch issues
        // on the same shipment surface a conflict (409) instead of enqueueing duplicate outbox
        // submissions (F9). Idempotent ADD COLUMN pattern (Phase117/Phase119).
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE shipments ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE shipments DROP COLUMN IF EXISTS concurrency_token;");
        }
    }
}
