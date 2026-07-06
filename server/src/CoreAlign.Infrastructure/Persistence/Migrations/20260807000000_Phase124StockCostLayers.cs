using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase124StockCostLayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS stock_cost_layers (
    id uuid NOT NULL,
    stock_item_id uuid NOT NULL,
    product_id uuid NOT NULL,
    warehouse_id uuid NOT NULL,
    lot_id uuid NULL,
    unit_cost numeric(18,4) NOT NULL,
    original_quantity numeric(18,4) NOT NULL,
    remaining_quantity numeric(18,4) NOT NULL,
    received_at_utc timestamp with time zone NOT NULL,
    source_movement_id uuid NULL,
    concurrency_token bigint NOT NULL DEFAULT 0,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_stock_cost_layers PRIMARY KEY (id),
    CONSTRAINT fk_stock_cost_layers_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS ix_stock_cost_layers_source_movement_id ON stock_cost_layers (source_movement_id);
CREATE INDEX IF NOT EXISTS ix_stock_cost_layers_stock_item_id_received_at_utc ON stock_cost_layers (stock_item_id, received_at_utc);
CREATE INDEX IF NOT EXISTS ix_stock_cost_layers_tenant_id ON stock_cost_layers (tenant_id);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS stock_cost_layers;");
        }
    }
}
