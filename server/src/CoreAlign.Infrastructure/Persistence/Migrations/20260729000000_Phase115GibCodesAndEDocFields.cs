using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase115GibCodesAndEDocFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "withholding_code",
                table: "order_lines",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "withholding_denominator",
                table: "order_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "withholding_numerator",
                table: "order_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "withholding_tax_code_id",
                table: "order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "e_invoice_gib_status_code",
                table: "invoices",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "e_invoice_last_sync_utc",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "e_invoice_profile",
                table: "invoices",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "e_invoice_reject_reason",
                table: "invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "e_invoice_sent_at_utc",
                table: "invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vat_exemption_code",
                table: "invoices",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "vat_exemption_code_id",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vat_exemption_reason",
                table: "invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "withholding_code",
                table: "invoice_lines",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "withholding_denominator",
                table: "invoice_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "withholding_numerator",
                table: "invoice_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "withholding_tax_code_id",
                table: "invoice_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vat_exemption_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    law_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vat_exemption_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "withholding_tax_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    numerator = table.Column<int>(type: "integer", nullable: false),
                    denominator = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_withholding_tax_codes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_withholding_tax_code_id",
                table: "order_lines",
                column: "withholding_tax_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_vat_exemption_code_id",
                table: "invoices",
                column: "vat_exemption_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_withholding_tax_code_id",
                table: "invoice_lines",
                column: "withholding_tax_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_vat_exemption_codes_tenant_id_code",
                table: "vat_exemption_codes",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vat_exemption_codes_tenant_id_is_active",
                table: "vat_exemption_codes",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_withholding_tax_codes_tenant_id_code",
                table: "withholding_tax_codes",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_withholding_tax_codes_tenant_id_is_active",
                table: "withholding_tax_codes",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.AddForeignKey(
                name: "fk_invoice_lines_withholding_tax_codes_withholding_tax_code_id",
                table: "invoice_lines",
                column: "withholding_tax_code_id",
                principalTable: "withholding_tax_codes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_invoices_vat_exemption_codes_vat_exemption_code_id",
                table: "invoices",
                column: "vat_exemption_code_id",
                principalTable: "vat_exemption_codes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_order_lines_withholding_tax_codes_withholding_tax_code_id",
                table: "order_lines",
                column: "withholding_tax_code_id",
                principalTable: "withholding_tax_codes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                ALTER TABLE withholding_tax_codes DROP CONSTRAINT IF EXISTS ck_withholding_tax_codes_fraction;
                ALTER TABLE withholding_tax_codes ADD CONSTRAINT ck_withholding_tax_codes_fraction
                    CHECK (numerator > 0 AND denominator > 0 AND numerator <= denominator);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_invoice_lines_withholding_tax_codes_withholding_tax_code_id",
                table: "invoice_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_invoices_vat_exemption_codes_vat_exemption_code_id",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_order_lines_withholding_tax_codes_withholding_tax_code_id",
                table: "order_lines");

            migrationBuilder.DropTable(
                name: "vat_exemption_codes");

            migrationBuilder.DropTable(
                name: "withholding_tax_codes");

            migrationBuilder.DropIndex(
                name: "ix_order_lines_withholding_tax_code_id",
                table: "order_lines");

            migrationBuilder.DropIndex(
                name: "ix_invoices_vat_exemption_code_id",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_invoice_lines_withholding_tax_code_id",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "withholding_code",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "withholding_denominator",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "withholding_numerator",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "withholding_tax_code_id",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "e_invoice_gib_status_code",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "e_invoice_last_sync_utc",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "e_invoice_profile",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "e_invoice_reject_reason",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "e_invoice_sent_at_utc",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "vat_exemption_code",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "vat_exemption_code_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "vat_exemption_reason",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "withholding_code",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "withholding_denominator",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "withholding_numerator",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "withholding_tax_code_id",
                table: "invoice_lines");
        }
    }
}
