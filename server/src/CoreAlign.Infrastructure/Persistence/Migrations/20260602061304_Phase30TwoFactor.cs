using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase30TwoFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // tenants.require_two_factor_for_roles sutunu Phase31TaxDeclarations (timestamp daha kucuk, once apply edilir)
            // tarafindan zaten ekleniyor. Concurrent merge cakismasi — burada duplicate ekleme cikariliyor.

            migrationBuilder.CreateTable(
                name: "two_factor_backup_codes",
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
                    table.PrimaryKey("pk_two_factor_backup_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_two_factor_backup_codes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "two_factor_challenges",
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
                    table.PrimaryKey("pk_two_factor_challenges", x => x.id);
                    table.ForeignKey(
                        name: "fk_two_factor_challenges_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_backup_codes_tenant_id_user_id",
                table: "two_factor_backup_codes",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_backup_codes_tenant_id_user_id_code_hash",
                table: "two_factor_backup_codes",
                columns: new[] { "tenant_id", "user_id", "code_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_backup_codes_user_id",
                table: "two_factor_backup_codes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_challenges_tenant_id_user_id",
                table: "two_factor_challenges",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_challenges_token_hash",
                table: "two_factor_challenges",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_two_factor_challenges_user_id",
                table: "two_factor_challenges",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "two_factor_backup_codes");

            migrationBuilder.DropTable(
                name: "two_factor_challenges");

            // require_two_factor_for_roles DropColumn'u Phase31'in Down'u sahipleniyor — burada duplicate cikariliyor.
        }
    }
}
