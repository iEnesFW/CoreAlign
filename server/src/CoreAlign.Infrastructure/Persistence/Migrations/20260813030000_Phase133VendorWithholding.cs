using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase133VendorWithholding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE vendor_bills ADD COLUMN IF NOT EXISTS withholding_code character varying(16);");
            migrationBuilder.Sql("ALTER TABLE vendor_bills ADD COLUMN IF NOT EXISTS withholding_numerator integer NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE vendor_bills ADD COLUMN IF NOT EXISTS withholding_denominator integer NOT NULL DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE vendor_bills DROP COLUMN IF EXISTS withholding_code;");
            migrationBuilder.Sql("ALTER TABLE vendor_bills DROP COLUMN IF EXISTS withholding_numerator;");
            migrationBuilder.Sql("ALTER TABLE vendor_bills DROP COLUMN IF EXISTS withholding_denominator;");
        }
    }
}
