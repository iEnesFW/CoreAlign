using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase40PlatformFxAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_archived",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "archived_at_utc",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "archived_by_user_id",
                table: "tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenants_is_archived",
                table: "tenants",
                column: "is_archived");

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "name", "description", "is_active", "created_at_utc" },
                values: new object[] { 3, "PlatformAdmin", "Cross-tenant platform administrator. Granted via DemoDataSeeder / runbook.", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    rate_against_try = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    valid_on_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fetched_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_rates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_exchange_rates_tenant_id_currency_valid_on_date",
                table: "exchange_rates",
                columns: new[] { "tenant_id", "currency", "valid_on_date" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "entity_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rolling_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entity_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_entity_audit_logs_tenant_id_entity_type_entity_id_changed_a~",
                table: "entity_audit_logs",
                columns: new[] { "tenant_id", "entity_type", "entity_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_entity_audit_logs_tenant_id_sequence",
                table: "entity_audit_logs",
                columns: new[] { "tenant_id", "sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "entity_audit_logs");
            migrationBuilder.DropTable(name: "exchange_rates");

            migrationBuilder.DeleteData(table: "roles", keyColumn: "id", keyValue: 3);

            migrationBuilder.DropIndex(name: "ix_tenants_is_archived", table: "tenants");
            migrationBuilder.DropColumn(name: "archived_by_user_id", table: "tenants");
            migrationBuilder.DropColumn(name: "archived_at_utc", table: "tenants");
            migrationBuilder.DropColumn(name: "is_archived", table: "tenants");
        }
    }
}
