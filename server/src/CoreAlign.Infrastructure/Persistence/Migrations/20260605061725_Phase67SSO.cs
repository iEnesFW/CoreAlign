using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase67SSO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_user_bindings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    local_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_user_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    external_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    last_login_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_user_bindings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_identity_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    protocol = table.Column<int>(type: "integer", nullable: false),
                    entity_id_or_client_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    metadata_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    discovery_document_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    client_secret_encrypted = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    attribute_mappings_json = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_used_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_identity_providers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_external_user_bindings_tenant_user",
                table: "external_user_bindings",
                columns: new[] { "tenant_id", "local_user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_external_user_bindings_idp_external",
                table: "external_user_bindings",
                columns: new[] { "identity_provider_id", "external_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_identity_providers_tenant_active",
                table: "tenant_identity_providers",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_tenant_identity_providers_tenant_name",
                table: "tenant_identity_providers",
                columns: new[] { "tenant_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_user_bindings");

            migrationBuilder.DropTable(
                name: "tenant_identity_providers");
        }
    }
}
