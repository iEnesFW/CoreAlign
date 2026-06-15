using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase17BillingInfoAndIyzico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "billing_address",
                table: "subscription_orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_city",
                table: "subscription_orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_country",
                table: "subscription_orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_zip_code",
                table: "subscription_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "buyer_email",
                table: "subscription_orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "buyer_gsm_number",
                table: "subscription_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "buyer_identity_number",
                table: "subscription_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "buyer_ip_address",
                table: "subscription_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "buyer_name",
                table: "subscription_orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "buyer_surname",
                table: "subscription_orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_transaction_id",
                table: "subscription_orders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "billing_address",
                table: "subscription_orders");

            migrationBuilder.DropColumn(
                name: "billing_city",
                table: "subscription_orders");

            migrationBuilder.DropColumn(
                name: "billing_country",
                table: "subscription_orders");

            migrationBuilder.DropColumn(
                name: "billing_zip_code",
                table: "subscription_orders");

            migrationBuilder.DropColumn(
                name: "buyer_email",
                table: "subscription_orders");

            migrationBuilder.DropColumn(
                name: "buyer_gsm_number",
                table: "subscription_orders");

            migrationBuilder.DropColumn(
                name: "buyer_identity_number",
                table: "subscription_orders");

            migrationBuilder.DropColumn(
                name: "buyer_ip_address",
                table: "subscription_orders");

            migrationBuilder.DropColumn(
                name: "buyer_name",
                table: "subscription_orders");

            migrationBuilder.DropColumn(
                name: "buyer_surname",
                table: "subscription_orders");

            migrationBuilder.DropColumn(
                name: "payment_transaction_id",
                table: "subscription_orders");
        }
    }
}
