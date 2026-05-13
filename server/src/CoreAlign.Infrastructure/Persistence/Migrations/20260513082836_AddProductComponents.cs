using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    component_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_components", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_components_products_component_product_id",
                        column: x => x.component_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_components_products_parent_product_id",
                        column: x => x.parent_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_components_component_product_id",
                table: "product_components",
                column: "component_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_components_parent_component_unique",
                table: "product_components",
                columns: new[] { "tenant_id", "parent_product_id", "component_product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_components_parent_product_id",
                table: "product_components",
                column: "parent_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_components_tenant_id_parent_product_id",
                table: "product_components",
                columns: new[] { "tenant_id", "parent_product_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_components");
        }
    }
}
