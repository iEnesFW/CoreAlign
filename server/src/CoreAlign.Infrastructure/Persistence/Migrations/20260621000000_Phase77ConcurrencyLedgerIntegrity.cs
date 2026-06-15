using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase77ConcurrencyLedgerIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_transactions_customers_customer_id",
                table: "customer_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transactions_products_product_id",
                table: "stock_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_vendor_ledger_entries_vendors_vendor_id",
                table: "vendor_ledger_entries");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "vendor_payments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "vendor_ledger_entries",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "vendor_bills",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "payments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "orders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "journal_entries",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "invoices",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "customer_ledger_entries",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_account_id",
                table: "journal_lines",
                column: "account_id");

            migrationBuilder.AddForeignKey(
                name: "fk_customer_transactions_customers_customer_id",
                table: "customer_transactions",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_journal_lines_gl_accounts_account_id",
                table: "journal_lines",
                column: "account_id",
                principalTable: "gl_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transactions_products_product_id",
                table: "stock_transactions",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vendor_ledger_entries_vendors_vendor_id",
                table: "vendor_ledger_entries",
                column: "vendor_id",
                principalTable: "vendors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_transactions_customers_customer_id",
                table: "customer_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_journal_lines_gl_accounts_account_id",
                table: "journal_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_transactions_products_product_id",
                table: "stock_transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_vendor_ledger_entries_vendors_vendor_id",
                table: "vendor_ledger_entries");

            migrationBuilder.DropIndex(
                name: "ix_journal_lines_account_id",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "vendor_payments");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "vendor_bills");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "customer_ledger_entries");

            migrationBuilder.AddForeignKey(
                name: "fk_customer_transactions_customers_customer_id",
                table: "customer_transactions",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_transactions_products_product_id",
                table: "stock_transactions",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_vendor_ledger_entries_vendors_vendor_id",
                table: "vendor_ledger_entries",
                column: "vendor_id",
                principalTable: "vendors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
