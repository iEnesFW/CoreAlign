using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase44StockAvailabilityAndSubstitute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_substitutes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    substitute_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversion_rate = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    is_bidirectional = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_substitutes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_substitutes_tenant_id_product_id_priority",
                table: "product_substitutes",
                columns: new[] { "tenant_id", "product_id", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_product_substitutes_tenant_id_product_id_substitute_product~",
                table: "product_substitutes",
                columns: new[] { "tenant_id", "product_id", "substitute_product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_substitutes");
        }
    }
}
