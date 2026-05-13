using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4InvoicePaymentLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invoices_order_id",
                table: "invoices");

            migrationBuilder.AddColumn<decimal>(
                name: "amount_paid",
                table: "invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_user_id",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_address_snapshot",
                table: "invoices",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancel_reason",
                table: "invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "credit_note_id",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_snapshot",
                table: "invoices",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "e_invoice_pdf_path",
                table: "invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "e_invoice_status",
                table: "invoices",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "e_invoice_uuid",
                table: "invoices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate",
                table: "invoices",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "header_discount_amount",
                table: "invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "header_discount_percent",
                table: "invoices",
                type: "numeric(6,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "internal_notes",
                table: "invoices",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_period_locked",
                table: "invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_posted_to_ledger",
                table: "invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "issued_at_utc",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_total",
                table: "invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "origin_invoice_id",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_terms_id",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "payment_terms_net_days_snapshot",
                table: "invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "posting_date",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "public_notes",
                table: "invoices",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rounding_adjustment",
                table: "invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "sent_at_utc",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_address_snapshot",
                table: "invoices",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_cost",
                table: "invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "tax_breakdown_json",
                table: "invoices",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_total",
                table: "invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "taxable_total",
                table: "invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "terms_and_conditions",
                table: "invoices",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "void_reason",
                table: "invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "voided_at_utc",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "withholding_total",
                table: "invoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_id",
                table: "invoice_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "cost_center",
                table: "invoice_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "invoice_lines",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_tax_inclusive",
                table: "invoice_lines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_amount",
                table: "invoice_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_discount_percent",
                table: "invoice_lines",
                type: "numeric(6,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_net_amount",
                table: "invoice_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "line_number",
                table: "invoice_lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "line_subtotal",
                table: "invoice_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_total",
                table: "invoice_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "origin_order_line_id",
                table: "invoice_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "project",
                table: "invoice_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revenue_account_code",
                table: "invoice_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount",
                table: "invoice_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tax_rate_id",
                table: "invoice_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_rate_percent",
                table: "invoice_lines",
                type: "numeric(6,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "uom_code",
                table: "invoice_lines",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "uom_id",
                table: "invoice_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "withholding_amount",
                table: "invoice_lines",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "withholding_rate_percent",
                table: "invoice_lines",
                type: "numeric(6,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "customer_ledger_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posting_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    entry_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    amount_in_base = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    source_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_document_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    running_balance_after = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_ledger_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_ledger_entries_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    direction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posting_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    applied_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    bank_account_info = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    check_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    check_due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_applications", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_applications_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_applications_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_order_id",
                table: "invoices",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_id_due_date",
                table: "invoices",
                columns: new[] { "tenant_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_id_status",
                table: "invoices",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_origin_order_line_id",
                table: "invoice_lines",
                column: "origin_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_ledger_entries_customer_id",
                table: "customer_ledger_entries",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_ledger_entries_tenant_id_customer_id_occurred_at_u~",
                table: "customer_ledger_entries",
                columns: new[] { "tenant_id", "customer_id", "occurred_at_utc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_customer_ledger_entries_tenant_id_source_type_source_docume~",
                table: "customer_ledger_entries",
                columns: new[] { "tenant_id", "source_type", "source_document_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_applications_invoice_id",
                table: "payment_applications",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_applications_payment_id",
                table: "payment_applications",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_applications_tenant_id_invoice_id",
                table: "payment_applications",
                columns: new[] { "tenant_id", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_applications_tenant_id_payment_id",
                table: "payment_applications",
                columns: new[] { "tenant_id", "payment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_customer_id",
                table: "payments",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_id_customer_id",
                table: "payments",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_id_payment_date",
                table: "payments",
                columns: new[] { "tenant_id", "payment_date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_id_payment_number",
                table: "payments",
                columns: new[] { "tenant_id", "payment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_id_status",
                table: "payments",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_ledger_entries");

            migrationBuilder.DropTable(
                name: "payment_applications");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropIndex(
                name: "ix_invoices_order_id",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_invoices_tenant_id_due_date",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_invoices_tenant_id_status",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_invoice_lines_origin_order_line_id",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "amount_paid",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "approved_by_user_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "billing_address_snapshot",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "cancel_reason",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "credit_note_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "customer_snapshot",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "e_invoice_pdf_path",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "e_invoice_status",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "e_invoice_uuid",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "exchange_rate",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "header_discount_amount",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "header_discount_percent",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "internal_notes",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "is_period_locked",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "is_posted_to_ledger",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "issued_at_utc",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "line_discount_total",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "origin_invoice_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "payment_terms_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "payment_terms_net_days_snapshot",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "posting_date",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "public_notes",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "rounding_adjustment",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "sent_at_utc",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "shipping_address_snapshot",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "shipping_cost",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "tax_breakdown_json",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "tax_total",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "taxable_total",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "terms_and_conditions",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "type",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "void_reason",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "voided_at_utc",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "withholding_total",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "cost_center",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "description",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "is_tax_inclusive",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "line_discount_amount",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "line_discount_percent",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "line_net_amount",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "line_number",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "line_subtotal",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "line_total",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "origin_order_line_id",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "project",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "revenue_account_code",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "tax_amount",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "tax_rate_id",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "tax_rate_percent",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "uom_code",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "uom_id",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "withholding_amount",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "withholding_rate_percent",
                table: "invoice_lines");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_id",
                table: "invoice_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_order_id",
                table: "invoices",
                column: "order_id",
                unique: true,
                filter: "\"order_id\" IS NOT NULL");
        }
    }
}
