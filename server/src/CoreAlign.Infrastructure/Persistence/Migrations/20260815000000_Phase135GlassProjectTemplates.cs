using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase135GlassProjectTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS glass_project_templates (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    created_by_user_id uuid NOT NULL,
    payload_json jsonb NOT NULL,
    wall_count integer NOT NULL,
    slab_count integer NOT NULL,
    run_count integer NOT NULL,
    concurrency_token bigint NOT NULL DEFAULT 0,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_glass_project_templates PRIMARY KEY (id),
    CONSTRAINT fk_glass_project_templates_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS ix_glass_project_templates_tenant_user_updated ON glass_project_templates (tenant_id, created_by_user_id, updated_at_utc);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS glass_project_templates;");
        }
    }
}
