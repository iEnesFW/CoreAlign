using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase62Whitelabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_theme_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    public_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_theme_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_themes",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    logo_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    favicon_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    primary_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    accent_color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    brand_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    custom_subdomain = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    custom_domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email_from_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email_from_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    login_background_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    login_heading_md = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_themes", x => x.tenant_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_theme_assets_tenant_kind",
                table: "tenant_theme_assets",
                columns: new[] { "tenant_id", "asset_kind" });

            migrationBuilder.CreateIndex(
                name: "ux_tenant_themes_custom_domain",
                table: "tenant_themes",
                column: "custom_domain",
                unique: true,
                filter: "custom_domain IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_tenant_themes_custom_subdomain",
                table: "tenant_themes",
                column: "custom_subdomain",
                unique: true,
                filter: "custom_subdomain IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_theme_assets");

            migrationBuilder.DropTable(
                name: "tenant_themes");
        }
    }
}
