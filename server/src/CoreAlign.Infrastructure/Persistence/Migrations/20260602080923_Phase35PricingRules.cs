using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase35PricingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pricing_discount_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    customer_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valid_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    min_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    value_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_discount_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pricing_tax_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    region_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    product_class = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    product_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rate_percent = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    fallback_tax_rate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    valid_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_tax_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pricing_discount_rules_tenant_id_code",
                table: "pricing_discount_rules",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pricing_discount_rules_tenant_id_is_active",
                table: "pricing_discount_rules",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_pricing_discount_rules_tenant_id_scope",
                table: "pricing_discount_rules",
                columns: new[] { "tenant_id", "scope" });

            migrationBuilder.CreateIndex(
                name: "ix_pricing_tax_rules_tenant_id_code",
                table: "pricing_tax_rules",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pricing_tax_rules_tenant_id_is_active",
                table: "pricing_tax_rules",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_pricing_tax_rules_tenant_id_scope",
                table: "pricing_tax_rules",
                columns: new[] { "tenant_id", "scope" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pricing_discount_rules");

            migrationBuilder.DropTable(
                name: "pricing_tax_rules");
        }
    }
}
