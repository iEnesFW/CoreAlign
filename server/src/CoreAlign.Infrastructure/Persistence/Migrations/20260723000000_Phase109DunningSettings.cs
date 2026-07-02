using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase109DunningSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS dunning_settings (
                    id uuid NOT NULL,
                    type character varying(32) NOT NULL,
                    is_enabled boolean NOT NULL,
                    send_in_app boolean NOT NULL,
                    send_email boolean NOT NULL,
                    recipient_user_ids_json text NOT NULL,
                    tenant_id uuid NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT pk_dunning_settings PRIMARY KEY (id),
                    CONSTRAINT fk_dunning_settings_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
                );");
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS ix_dunning_settings_tenant_id_type ON dunning_settings (tenant_id, type);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS dunning_settings;");
        }
    }
}
