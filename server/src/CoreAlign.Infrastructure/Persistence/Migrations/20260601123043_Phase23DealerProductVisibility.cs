using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase23DealerProductVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_dealer_product_visibilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealer_customer_link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_dealer_product_visibilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_dealer_product_visibilities_dealer_customer_links_de~",
                        column: x => x.dealer_customer_link_id,
                        principalTable: "dealer_customer_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_dealer_product_visibilities_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cdpv_tenant_link",
                table: "customer_dealer_product_visibilities",
                columns: new[] { "tenant_id", "dealer_customer_link_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_dealer_product_visibilities_dealer_customer_link_id",
                table: "customer_dealer_product_visibilities",
                column: "dealer_customer_link_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_dealer_product_visibilities_product_id",
                table: "customer_dealer_product_visibilities",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_cdpv_tenant_link_product",
                table: "customer_dealer_product_visibilities",
                columns: new[] { "tenant_id", "dealer_customer_link_id", "product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_dealer_product_visibilities");
        }
    }
}
