using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase34QuotesAndStandaloneInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_quote_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            // Phase33'ten tasinmis ordering fix: source_quote_id sutunu eklendikten sonra index yarat.
            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_source_quote_id",
                table: "orders",
                columns: new[] { "tenant_id", "source_quote_id" },
                filter: "source_quote_id IS NOT NULL");

            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shipping_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    billing_address_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    shipping_address_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    quote_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expired_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    converted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    payment_terms_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_terms_net_days_snapshot = table.Column<int>(type: "integer", nullable: true),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_rep_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_discount_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    header_discount_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    header_discount_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    taxable_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tax_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    withholding_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    shipping_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    rounding_adjustment = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    converted_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    internal_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    customer_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    public_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    terms_and_conditions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotes", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotes_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quote_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    product_description_snapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    uom_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uom_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    uom_conversion_factor = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    list_price_snapshot = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_discount_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    line_discount_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_manual_price_override = table.Column<bool>(type: "boolean", nullable: false),
                    tax_rate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_rate_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_tax_inclusive = table.Column<bool>(type: "boolean", nullable: false),
                    withholding_rate_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    withholding_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_subtotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_net_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quote_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_quote_lines_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_quote_lines_quotes_quote_id",
                        column: x => x.quote_id,
                        principalTable: "quotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_quotes_customer_id",
                table: "quotes",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_tenant_id_customer_id",
                table: "quotes",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_quotes_tenant_id_quote_date",
                table: "quotes",
                columns: new[] { "tenant_id", "quote_date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_quotes_tenant_id_quote_number",
                table: "quotes",
                columns: new[] { "tenant_id", "quote_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotes_tenant_id_status",
                table: "quotes",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_quotes_tenant_id_valid_until_utc_status",
                table: "quotes",
                columns: new[] { "tenant_id", "valid_until_utc", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_quote_lines_product_id",
                table: "quote_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_quote_lines_quote_id",
                table: "quote_lines",
                column: "quote_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quote_lines");

            migrationBuilder.DropTable(
                name: "quotes");

            // Phase33'ten tasinmis ordering fix: index sutunu dropludan once kaldir.
            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id_source_quote_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "source_quote_id",
                table: "orders");
        }
    }
}
