using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase125SerialUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS stock_serial_units (
    id uuid NOT NULL,
    product_id uuid NOT NULL,
    serial_number character varying(100) NOT NULL,
    lot_id uuid NULL,
    warehouse_id uuid NULL,
    status character varying(20) NOT NULL,
    unit_cost numeric(18,4) NOT NULL,
    received_at_utc timestamp with time zone NOT NULL,
    source_receipt_movement_id uuid NULL,
    order_id uuid NULL,
    shipment_id uuid NULL,
    current_owner_customer_id uuid NULL,
    parent_serial_unit_id uuid NULL,
    concurrency_token bigint NOT NULL DEFAULT 0,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_stock_serial_units PRIMARY KEY (id),
    CONSTRAINT fk_stock_serial_units_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS ix_stock_serial_units_parent_serial_unit_id ON stock_serial_units (parent_serial_unit_id);
CREATE UNIQUE INDEX IF NOT EXISTS ix_stock_serial_units_tenant_id_product_id_serial_number ON stock_serial_units (tenant_id, product_id, serial_number);
CREATE INDEX IF NOT EXISTS ix_stock_serial_units_tenant_id_serial_number ON stock_serial_units (tenant_id, serial_number);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS stock_serial_units;");
        }
    }
}
