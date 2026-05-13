using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrigramSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_customers_name_trgm ON customers USING gin (lower(name) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_customers_email_trgm ON customers USING gin (lower(email) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_customers_phone_trgm ON customers USING gin (lower(phone) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_customers_tax_number_trgm ON customers USING gin (lower(tax_number) gin_trgm_ops);");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_products_sku_trgm ON products USING gin (lower(sku) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_products_name_trgm ON products USING gin (lower(name) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_products_description_trgm ON products USING gin (lower(description) gin_trgm_ops);");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_orders_order_number_trgm ON orders USING gin (lower(order_number) gin_trgm_ops);");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_invoices_invoice_number_trgm ON invoices USING gin (lower(invoice_number) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_invoices_customer_name_snapshot_trgm ON invoices USING gin (lower(customer_name_snapshot) gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_customers_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_customers_email_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_customers_phone_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_customers_tax_number_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_products_sku_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_products_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_products_description_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_orders_order_number_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_invoices_invoice_number_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_invoices_customer_name_snapshot_trgm;");
        }
    }
}
