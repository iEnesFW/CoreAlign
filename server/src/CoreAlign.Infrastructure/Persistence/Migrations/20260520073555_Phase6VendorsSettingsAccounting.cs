using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase6VendorsSettingsAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address_line1",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_line2",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_currency",
                table: "tenants",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "tenants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fax",
                table: "tenants",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fiscal_year_start_month",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "founded_on",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_name",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locale_code",
                table: "tenants",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                table: "tenants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mersis_number",
                table: "tenants",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "national_id",
                table: "tenants",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "tenants",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "tenants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_color",
                table: "tenants",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reporting_currency",
                table: "tenants",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_color",
                table: "tenants",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sector",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state_province",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_number",
                table: "tenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_office",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "time_zone_id",
                table: "tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "trade_name",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trade_registry_number",
                table: "tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website",
                table: "tenants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    available_variables = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gl_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    normal_side = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    is_postable = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gl_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_gl_accounts_gl_accounts_parent_id",
                        column: x => x.parent_id,
                        principalTable: "gl_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    entry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posting_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    total_debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    posted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    posted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reversed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reversed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reversal_of_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reversed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_settings_store",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    data_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_settings_store", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vendors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    trade_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    national_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    tax_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tax_office = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    default_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payment_terms_id = table.Column<Guid>(type: "uuid", nullable: true),
                    buyer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    current_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    overdue_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_payable = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    classification = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    territory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    parent_vendor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    block_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    approved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendors", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendors_payment_terms_payment_terms_id",
                        column: x => x.payment_terms_id,
                        principalTable: "payment_terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_vendors_vendors_parent_vendor_id",
                        column: x => x.parent_vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cost_center = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    project = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    foreign_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_journal_lines_journal_entries_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendor_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_addresses_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendor_bank_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    branch_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    account_holder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    swift = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    account_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_bank_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_bank_accounts_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendor_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendor_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_contacts_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendor_ledger_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    posting_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    entry_type = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    amount_in_base = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("pk_vendor_ledger_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_vendor_ledger_entries_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_templates_tenant_id_code_locale",
                table: "email_templates",
                columns: new[] { "tenant_id", "code", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gl_accounts_parent_id",
                table: "gl_accounts",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_gl_accounts_tenant_id_code",
                table: "gl_accounts",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gl_accounts_tenant_id_parent_id",
                table: "gl_accounts",
                columns: new[] { "tenant_id", "parent_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gl_accounts_tenant_id_type_is_active",
                table: "gl_accounts",
                columns: new[] { "tenant_id", "type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_tenant_id_number",
                table: "journal_entries",
                columns: new[] { "tenant_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_tenant_id_posting_date",
                table: "journal_entries",
                columns: new[] { "tenant_id", "posting_date" });

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_tenant_id_status_posting_date",
                table: "journal_entries",
                columns: new[] { "tenant_id", "status", "posting_date" });

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_tenant_id_type",
                table: "journal_entries",
                columns: new[] { "tenant_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_journal_entry_id",
                table: "journal_lines",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_tenant_id_account_id",
                table: "journal_lines",
                columns: new[] { "tenant_id", "account_id" });

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_tenant_id_journal_entry_id_line_number",
                table: "journal_lines",
                columns: new[] { "tenant_id", "journal_entry_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_settings_store_tenant_id_category",
                table: "tenant_settings_store",
                columns: new[] { "tenant_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_settings_store_tenant_id_category_key",
                table: "tenant_settings_store",
                columns: new[] { "tenant_id", "category", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_addresses_tenant_id_vendor_id",
                table: "vendor_addresses",
                columns: new[] { "tenant_id", "vendor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_addresses_vendor_id",
                table: "vendor_addresses",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_bank_accounts_tenant_id_iban",
                table: "vendor_bank_accounts",
                columns: new[] { "tenant_id", "iban" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_bank_accounts_tenant_id_vendor_id",
                table: "vendor_bank_accounts",
                columns: new[] { "tenant_id", "vendor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_bank_accounts_vendor_id",
                table: "vendor_bank_accounts",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_contacts_tenant_id_vendor_id",
                table: "vendor_contacts",
                columns: new[] { "tenant_id", "vendor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_contacts_vendor_id",
                table: "vendor_contacts",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_ledger_entries_tenant_id_source_type_source_document~",
                table: "vendor_ledger_entries",
                columns: new[] { "tenant_id", "source_type", "source_document_id" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_ledger_entries_tenant_id_vendor_id_occurred_at_utc",
                table: "vendor_ledger_entries",
                columns: new[] { "tenant_id", "vendor_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_ledger_entries_tenant_id_vendor_id_posting_date",
                table: "vendor_ledger_entries",
                columns: new[] { "tenant_id", "vendor_id", "posting_date" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_ledger_entries_vendor_id",
                table: "vendor_ledger_entries",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendors_parent_vendor_id",
                table: "vendors",
                column: "parent_vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendors_payment_terms_id",
                table: "vendors",
                column: "payment_terms_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendors_tenant_id_code",
                table: "vendors",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "\"code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vendors_tenant_id_name",
                table: "vendors",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_vendors_tenant_id_status",
                table: "vendors",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_vendors_tenant_id_tax_number",
                table: "vendors",
                columns: new[] { "tenant_id", "tax_number" },
                filter: "\"tax_number\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_templates");

            migrationBuilder.DropTable(
                name: "gl_accounts");

            migrationBuilder.DropTable(
                name: "journal_lines");

            migrationBuilder.DropTable(
                name: "tenant_settings_store");

            migrationBuilder.DropTable(
                name: "vendor_addresses");

            migrationBuilder.DropTable(
                name: "vendor_bank_accounts");

            migrationBuilder.DropTable(
                name: "vendor_contacts");

            migrationBuilder.DropTable(
                name: "vendor_ledger_entries");

            migrationBuilder.DropTable(
                name: "journal_entries");

            migrationBuilder.DropTable(
                name: "vendors");

            migrationBuilder.DropColumn(
                name: "address_line1",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "address_line2",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "city",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "country",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "default_currency",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "email",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "fax",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "fiscal_year_start_month",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "founded_on",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "legal_name",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "locale_code",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "logo_url",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "mersis_number",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "national_id",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "primary_color",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "reporting_currency",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "secondary_color",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "sector",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "state_province",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "tax_number",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "tax_office",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "time_zone_id",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "trade_name",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "trade_registry_number",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "website",
                table: "tenants");
        }
    }
}
