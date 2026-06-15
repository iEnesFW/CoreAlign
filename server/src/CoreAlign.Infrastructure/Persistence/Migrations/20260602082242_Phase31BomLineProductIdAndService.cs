using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase31BomLineProductIdAndService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cut_spec_json",
                table: "glass_project_bom_lines",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_service",
                table: "glass_project_bom_lines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                table: "glass_project_bom_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_bom_lines_product_id",
                table: "glass_project_bom_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_glass_project_bom_lines_tenant_id_product_id",
                table: "glass_project_bom_lines",
                columns: new[] { "tenant_id", "product_id" },
                filter: "product_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_glass_project_bom_lines_products_product_id",
                table: "glass_project_bom_lines",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_glass_project_bom_lines_products_product_id",
                table: "glass_project_bom_lines");

            migrationBuilder.DropIndex(
                name: "ix_glass_project_bom_lines_product_id",
                table: "glass_project_bom_lines");

            migrationBuilder.DropIndex(
                name: "ix_glass_project_bom_lines_tenant_id_product_id",
                table: "glass_project_bom_lines");

            migrationBuilder.DropColumn(
                name: "cut_spec_json",
                table: "glass_project_bom_lines");

            migrationBuilder.DropColumn(
                name: "is_service",
                table: "glass_project_bom_lines");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "glass_project_bom_lines");
        }
    }
}
