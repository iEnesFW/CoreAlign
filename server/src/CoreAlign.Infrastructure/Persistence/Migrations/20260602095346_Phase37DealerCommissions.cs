using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase37DealerCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "commission_percent",
                table: "dealer_accounts",
                type: "numeric(7,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "commission_percent_override",
                table: "dealer_customer_links",
                type: "numeric(7,4)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dealer_commission_ledger_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealer_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    order_total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    commission_percent = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    accrued_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_out_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dealer_commission_ledger_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dealer_commission_ledger_entries_tenant_dealer_accrued_at_utc",
                table: "dealer_commission_ledger_entries",
                columns: new[] { "tenant_id", "dealer_account_id", "accrued_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_dealer_commission_ledger_entries_tenant_dealer_status",
                table: "dealer_commission_ledger_entries",
                columns: new[] { "tenant_id", "dealer_account_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_dealer_commission_ledger_entries_tenant_dealer_order_shipment",
                table: "dealer_commission_ledger_entries",
                columns: new[] { "tenant_id", "dealer_account_id", "order_id", "shipment_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dealer_commission_ledger_entries");

            migrationBuilder.DropColumn(
                name: "commission_percent",
                table: "dealer_accounts");

            migrationBuilder.DropColumn(
                name: "commission_percent_override",
                table: "dealer_customer_links");
        }
    }
}
