using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase5AccountingPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounting_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    closed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reopened_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reopened_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_periods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_product_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    min_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    max_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    valid_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_product_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_product_prices_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_product_prices_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_tenant_id_status",
                table: "accounting_periods",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_tenant_id_year_month",
                table: "accounting_periods",
                columns: new[] { "tenant_id", "year", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_product_prices_customer_id",
                table: "customer_product_prices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_product_prices_product_id",
                table: "customer_product_prices",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_product_prices_tenant_id_customer_id_product_id",
                table: "customer_product_prices",
                columns: new[] { "tenant_id", "customer_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_product_prices_tenant_id_product_id_is_active",
                table: "customer_product_prices",
                columns: new[] { "tenant_id", "product_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounting_periods");

            migrationBuilder.DropTable(
                name: "customer_product_prices");
        }
    }
}
