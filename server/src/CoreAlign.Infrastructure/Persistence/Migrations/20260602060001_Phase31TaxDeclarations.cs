using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase31TaxDeclarations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "require_two_factor_for_roles",
                table: "tenants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tax_declarations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    declaration_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    withholding_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    xml_payload = table.Column<string>(type: "text", nullable: true),
                    generated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    line_count = table.Column<int>(type: "integer", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_declarations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "two_factor_backup_code",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    used_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_two_factor_backup_code", x => x.id);
                    table.ForeignKey(
                        name: "fk_two_factor_backup_code_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "two_factor_challenge",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_two_factor_challenge", x => x.id);
                    table.ForeignKey(
                        name: "fk_two_factor_challenge_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tax_declaration_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_declaration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counterparty_tax_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    counterparty_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    document_count = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_declaration_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_tax_declaration_lines_tax_declarations_tax_declaration_id",
                        column: x => x.tax_declaration_id,
                        principalTable: "tax_declarations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tax_declaration_lines_tax_declaration_id",
                table: "tax_declaration_lines",
                column: "tax_declaration_id");

            migrationBuilder.CreateIndex(
                name: "ix_tax_declaration_lines_tenant_id_tax_declaration_id",
                table: "tax_declaration_lines",
                columns: new[] { "tenant_id", "tax_declaration_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tax_declarations_tenant_id_status",
                table: "tax_declarations",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_tax_declarations_tenant_id_year_month_declaration_type",
                table: "tax_declarations",
                columns: new[] { "tenant_id", "year", "month", "declaration_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_backup_code_tenant_id_user_id",
                table: "two_factor_backup_code",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_backup_code_tenant_id_user_id_code_hash",
                table: "two_factor_backup_code",
                columns: new[] { "tenant_id", "user_id", "code_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_backup_code_user_id",
                table: "two_factor_backup_code",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_challenge_tenant_id_user_id",
                table: "two_factor_challenge",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_challenge_token_hash",
                table: "two_factor_challenge",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_challenge_user_id",
                table: "two_factor_challenge",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tax_declaration_lines");

            migrationBuilder.DropTable(
                name: "two_factor_backup_code");

            migrationBuilder.DropTable(
                name: "two_factor_challenge");

            migrationBuilder.DropTable(
                name: "tax_declarations");

            migrationBuilder.DropColumn(
                name: "require_two_factor_for_roles",
                table: "tenants");
        }
    }
}
