using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase112AdvancePaymentFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE payments ADD COLUMN IF NOT EXISTS is_advance boolean NOT NULL DEFAULT false;");
            migrationBuilder.Sql("ALTER TABLE vendor_payments ADD COLUMN IF NOT EXISTS is_advance boolean NOT NULL DEFAULT false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE vendor_payments DROP COLUMN IF EXISTS is_advance;");
            migrationBuilder.Sql("ALTER TABLE payments DROP COLUMN IF EXISTS is_advance;");
        }
    }
}
