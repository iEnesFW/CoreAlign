using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase117ConcurrencyAndPaymentIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE vendor_payments ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE vendor_payments ADD COLUMN IF NOT EXISTS operation_id uuid NULL;");
            migrationBuilder.Sql("ALTER TABLE vendor_bills ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE purchase_orders ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_vendor_payments_tenant_operation ON vendor_payments (tenant_id, operation_id) WHERE operation_id IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_vendor_payments_tenant_operation;");
            migrationBuilder.Sql("ALTER TABLE vendor_payments DROP COLUMN IF EXISTS concurrency_token;");
            migrationBuilder.Sql("ALTER TABLE vendor_payments DROP COLUMN IF EXISTS operation_id;");
            migrationBuilder.Sql("ALTER TABLE vendor_bills DROP COLUMN IF EXISTS concurrency_token;");
            migrationBuilder.Sql("ALTER TABLE purchase_orders DROP COLUMN IF EXISTS concurrency_token;");
        }
    }
}
