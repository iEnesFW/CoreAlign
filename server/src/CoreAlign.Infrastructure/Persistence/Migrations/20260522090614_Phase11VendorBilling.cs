using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase11VendorBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vendor_bills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    bill_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    bill_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    posted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_bills", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_bills_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    payment_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    vendor_bill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_payments_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_bills_tenant_id_bill_date",
                table: "vendor_bills",
                columns: new[] { "tenant_id", "bill_date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_bills_tenant_id_status",
                table: "vendor_bills",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_bills_tenant_id_vendor_id_bill_number",
                table: "vendor_bills",
                columns: new[] { "tenant_id", "vendor_id", "bill_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_bills_vendor_id",
                table: "vendor_bills",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payments_tenant_id_payment_number",
                table: "vendor_payments",
                columns: new[] { "tenant_id", "payment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payments_tenant_id_vendor_bill_id",
                table: "vendor_payments",
                columns: new[] { "tenant_id", "vendor_bill_id" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payments_tenant_id_vendor_id",
                table: "vendor_payments",
                columns: new[] { "tenant_id", "vendor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_payments_vendor_id",
                table: "vendor_payments",
                column: "vendor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vendor_bills");

            migrationBuilder.DropTable(
                name: "vendor_payments");
        }
    }
}
