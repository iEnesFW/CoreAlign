using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyCustomerAddressColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO customer_addresses
                    (id, tenant_id, customer_id, label, line1, line2, city, state, postal_code, country, is_primary, created_at_utc, updated_at_utc)
                SELECT
                    gen_random_uuid(),
                    c.tenant_id,
                    c.id,
                    'Primary',
                    c.address,
                    NULL, NULL, NULL, NULL, NULL,
                    true,
                    now() AT TIME ZONE 'UTC',
                    now() AT TIME ZONE 'UTC'
                FROM customers c
                WHERE c.address IS NOT NULL
                  AND length(trim(c.address)) > 0
                  AND NOT EXISTS (
                      SELECT 1 FROM customer_addresses a
                      WHERE a.customer_id = c.id AND a.is_primary = true
                  );
            ");

            migrationBuilder.DropColumn(
                name: "address",
                table: "customers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
