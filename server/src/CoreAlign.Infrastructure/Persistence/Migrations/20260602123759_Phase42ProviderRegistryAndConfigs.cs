using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase42ProviderRegistryAndConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_webhook_inbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    signature_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload_json = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provider_webhook_inbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_provider_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    encrypted_credentials_json = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    enabled_capabilities = table.Column<int>(type: "integer", nullable: false),
                    last_health_check_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_health_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_health_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_provider_configs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_provider_webhook_inbox_signature_hash",
                table: "provider_webhook_inbox",
                column: "signature_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_provider_webhook_inbox_tenant_category_processed",
                table: "provider_webhook_inbox",
                columns: new[] { "tenant_id", "category", "processed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_provider_configs_tenant_category_provider",
                table: "tenant_provider_configs",
                columns: new[] { "tenant_id", "category", "provider_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_provider_configs_unique_default_per_category",
                table: "tenant_provider_configs",
                columns: new[] { "tenant_id", "category", "is_default" },
                unique: true,
                filter: "is_default = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_webhook_inbox");

            migrationBuilder.DropTable(
                name: "tenant_provider_configs");
        }
    }
}
