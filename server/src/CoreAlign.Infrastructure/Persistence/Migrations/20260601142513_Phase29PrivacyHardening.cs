using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase29PrivacyHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "concurrency_token",
                table: "customers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_approved_by_user_id",
                table: "orders",
                columns: new[] { "tenant_id", "approved_by_user_id" },
                filter: "approved_by_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_origin_customer_user_id",
                table: "orders",
                columns: new[] { "tenant_id", "origin_customer_user_id" },
                filter: "origin_customer_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_origin_dealer_user_id",
                table: "orders",
                columns: new[] { "tenant_id", "origin_dealer_user_id" },
                filter: "origin_dealer_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_sales_rep_user_id",
                table: "orders",
                columns: new[] { "tenant_id", "sales_rep_user_id" },
                filter: "sales_rep_user_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id_approved_by_user_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id_origin_customer_user_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id_origin_dealer_user_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id_sales_rep_user_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "customers");
        }
    }
}
