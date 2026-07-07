using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase129GlassPanelHardware : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS glass_project_panel_hardware (
    id uuid NOT NULL,
    panel_id uuid NOT NULL,
    hardware_item_id uuid NOT NULL,
    quantity numeric(12,3) NOT NULL,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_glass_project_panel_hardware PRIMARY KEY (id),
    CONSTRAINT fk_glass_project_panel_hardware_hardware_item_id FOREIGN KEY (hardware_item_id) REFERENCES glass_hardware_items (id) ON DELETE RESTRICT,
    CONSTRAINT fk_glass_project_panel_hardware_panel_id FOREIGN KEY (panel_id) REFERENCES glass_project_panels (id) ON DELETE CASCADE,
    CONSTRAINT fk_glass_project_panel_hardware_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS ix_glass_project_panel_hardware_hardware_item_id ON glass_project_panel_hardware (hardware_item_id);
CREATE INDEX IF NOT EXISTS ix_glass_project_panel_hardware_panel_id ON glass_project_panel_hardware (panel_id);
CREATE INDEX IF NOT EXISTS ix_glass_project_panel_hardware_tenant_id_panel_id ON glass_project_panel_hardware (tenant_id, panel_id);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS glass_project_panel_hardware;");
        }
    }
}
