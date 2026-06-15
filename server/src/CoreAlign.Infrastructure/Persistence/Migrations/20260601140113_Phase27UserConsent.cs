using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase27UserConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    anonymous_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    captured_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    withdrawn_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_consents", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_consents_tenant_id_anonymous_fingerprint_purpose",
                table: "user_consents",
                columns: new[] { "tenant_id", "anonymous_fingerprint", "purpose" });

            migrationBuilder.CreateIndex(
                name: "ix_user_consents_tenant_id_user_id_purpose_captured_at_utc",
                table: "user_consents",
                columns: new[] { "tenant_id", "user_id", "purpose", "captured_at_utc" },
                descending: new[] { false, false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_consents");
        }
    }
}
