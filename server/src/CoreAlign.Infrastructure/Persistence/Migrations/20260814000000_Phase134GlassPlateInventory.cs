using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase134GlassPlateInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE products ADD COLUMN IF NOT EXISTS is_plate_tracked boolean NOT NULL DEFAULT false;");
            migrationBuilder.Sql("ALTER TABLE products ADD COLUMN IF NOT EXISTS min_plate_count integer NULL;");
            migrationBuilder.Sql("ALTER TABLE products ADD COLUMN IF NOT EXISTS min_remnant_area_mm2 numeric(18,4) NULL;");
            migrationBuilder.Sql("ALTER TABLE products ADD COLUMN IF NOT EXISTS min_remnant_height_mm numeric(10,2) NULL;");
            migrationBuilder.Sql("ALTER TABLE products ADD COLUMN IF NOT EXISTS min_remnant_width_mm numeric(10,2) NULL;");
            migrationBuilder.Sql("ALTER TABLE products ADD COLUMN IF NOT EXISTS standard_height_mm numeric(10,2) NULL;");
            migrationBuilder.Sql("ALTER TABLE products ADD COLUMN IF NOT EXISTS standard_width_mm numeric(10,2) NULL;");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS storage_locations (
    id uuid NOT NULL,
    warehouse_id uuid NOT NULL,
    parent_location_id uuid NULL,
    code character varying(60) NOT NULL,
    name character varying(200) NOT NULL,
    kind character varying(20) NOT NULL,
    is_active boolean NOT NULL,
    notes character varying(1000) NULL,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_storage_locations PRIMARY KEY (id),
    CONSTRAINT ck_storage_locations_kind CHECK (kind IN ('Rack','Shelf','Pallet','Floor','Zone')),
    CONSTRAINT fk_storage_locations_storage_locations_parent_location_id FOREIGN KEY (parent_location_id) REFERENCES storage_locations (id) ON DELETE RESTRICT,
    CONSTRAINT fk_storage_locations_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT fk_storage_locations_warehouses_warehouse_id FOREIGN KEY (warehouse_id) REFERENCES warehouses (id) ON DELETE RESTRICT
);");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS user_warehouse_access (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    warehouse_id uuid NOT NULL,
    granted_by_user_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_user_warehouse_access PRIMARY KEY (id),
    CONSTRAINT fk_user_warehouse_access_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT fk_user_warehouse_access_warehouses_warehouse_id FOREIGN KEY (warehouse_id) REFERENCES warehouses (id) ON DELETE RESTRICT
);");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS glass_plates (
    id uuid NOT NULL,
    product_id uuid NOT NULL,
    warehouse_id uuid NOT NULL,
    storage_location_id uuid NULL,
    lot_id uuid NULL,
    plate_number character varying(60) NOT NULL,
    kind character varying(20) NOT NULL,
    status character varying(20) NOT NULL,
    width_mm numeric(10,2) NOT NULL,
    height_mm numeric(10,2) NOT NULL,
    thickness_mm numeric(9,2) NOT NULL,
    original_area_mm2 numeric(18,4) NOT NULL,
    remaining_area_mm2 numeric(18,4) NOT NULL,
    parent_plate_id uuid NULL,
    source_receipt_movement_id uuid NULL,
    reserved_by_job_id uuid NULL,
    condition character varying(20) NOT NULL,
    received_at_utc timestamp with time zone NOT NULL,
    consumed_at_utc timestamp with time zone NULL,
    notes character varying(1000) NULL,
    concurrency_token bigint NOT NULL DEFAULT 0,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_glass_plates PRIMARY KEY (id),
    CONSTRAINT ck_glass_plates_kind CHECK (kind IN ('Fresh','Remnant')),
    CONSTRAINT ck_glass_plates_status CHECK (status IN ('Available','Reserved','InUse','Consumed','Scrapped')),
    CONSTRAINT ck_glass_plates_condition CHECK (condition IN ('Good','Chipped','Cracked','Scratched')),
    CONSTRAINT ck_glass_plates_dimensions CHECK (width_mm > 0 AND height_mm > 0 AND original_area_mm2 >= 0 AND remaining_area_mm2 >= 0),
    CONSTRAINT fk_glass_plates_lots_lot_id FOREIGN KEY (lot_id) REFERENCES lots (id) ON DELETE SET NULL,
    CONSTRAINT fk_glass_plates_products_product_id FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE RESTRICT,
    CONSTRAINT fk_glass_plates_storage_locations_storage_location_id FOREIGN KEY (storage_location_id) REFERENCES storage_locations (id) ON DELETE SET NULL,
    CONSTRAINT fk_glass_plates_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT fk_glass_plates_warehouses_warehouse_id FOREIGN KEY (warehouse_id) REFERENCES warehouses (id) ON DELETE RESTRICT
);");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS glass_plate_consumptions (
    id uuid NOT NULL,
    glass_plate_id uuid NOT NULL,
    product_id uuid NOT NULL,
    warehouse_id uuid NOT NULL,
    order_line_id uuid NULL,
    job_id uuid NULL,
    cut_area_mm2 numeric(18,4) NOT NULL,
    pieces integer NOT NULL,
    cut_width_mm numeric(10,2) NULL,
    cut_height_mm numeric(10,2) NULL,
    resulting_remnant_plate_id uuid NULL,
    scrapped_area_mm2 numeric(18,4) NOT NULL,
    scrap_reason_code_id uuid NULL,
    work_center_id uuid NULL,
    operator_id uuid NULL,
    stock_movement_id uuid NULL,
    occurred_at_utc timestamp with time zone NOT NULL,
    posted_by_user_id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_glass_plate_consumptions PRIMARY KEY (id),
    CONSTRAINT ck_glass_plate_consumptions_amounts CHECK (cut_area_mm2 >= 0 AND scrapped_area_mm2 >= 0 AND pieces >= 0),
    CONSTRAINT fk_glass_plate_consumptions_glass_plates_glass_plate_id FOREIGN KEY (glass_plate_id) REFERENCES glass_plates (id) ON DELETE RESTRICT,
    CONSTRAINT fk_glass_plate_consumptions_products_product_id FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE RESTRICT,
    CONSTRAINT fk_glass_plate_consumptions_stock_reason_codes_reason FOREIGN KEY (scrap_reason_code_id) REFERENCES stock_reason_codes (id) ON DELETE SET NULL,
    CONSTRAINT fk_glass_plate_consumptions_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT,
    CONSTRAINT fk_glass_plate_consumptions_warehouses_warehouse_id FOREIGN KEY (warehouse_id) REFERENCES warehouses (id) ON DELETE RESTRICT
);");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_products_tenant_id_is_plate_tracked ON products (tenant_id, is_plate_tracked) WHERE is_plate_tracked = true;");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_storage_locations_parent_location_id ON storage_locations (parent_location_id);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ix_storage_locations_tenant_id_warehouse_id_code ON storage_locations (tenant_id, warehouse_id, code);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_storage_locations_warehouse_id ON storage_locations (warehouse_id);");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_user_warehouse_access_tenant_id_user_id ON user_warehouse_access (tenant_id, user_id);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ix_user_warehouse_access_tenant_id_user_id_warehouse_id ON user_warehouse_access (tenant_id, user_id, warehouse_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_user_warehouse_access_warehouse_id ON user_warehouse_access (warehouse_id);");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plates_lot_id ON glass_plates (lot_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plates_parent_plate_id ON glass_plates (parent_plate_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plates_product_id ON glass_plates (product_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plates_storage_location_id ON glass_plates (storage_location_id);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ix_glass_plates_tenant_id_plate_number ON glass_plates (tenant_id, plate_number);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plates_tenant_id_product_id_remaining_area_mm2 ON glass_plates (tenant_id, product_id, remaining_area_mm2) WHERE status = 'Available';");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plates_tenant_id_product_id_status ON glass_plates (tenant_id, product_id, status);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plates_tenant_id_warehouse_id_storage_location_id ON glass_plates (tenant_id, warehouse_id, storage_location_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plates_warehouse_id ON glass_plates (warehouse_id);");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plate_consumptions_glass_plate_id ON glass_plate_consumptions (glass_plate_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plate_consumptions_product_id ON glass_plate_consumptions (product_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plate_consumptions_scrap_reason_code_id ON glass_plate_consumptions (scrap_reason_code_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plate_consumptions_tenant_id_glass_plate_id ON glass_plate_consumptions (tenant_id, glass_plate_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plate_consumptions_tenant_id_order_line_id ON glass_plate_consumptions (tenant_id, order_line_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plate_consumptions_tenant_id_product_id_warehouse_id ON glass_plate_consumptions (tenant_id, product_id, warehouse_id);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_glass_plate_consumptions_warehouse_id ON glass_plate_consumptions (warehouse_id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS glass_plate_consumptions;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS glass_plates;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS user_warehouse_access;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS storage_locations;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_products_tenant_id_is_plate_tracked;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS is_plate_tracked;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS min_plate_count;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS min_remnant_area_mm2;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS min_remnant_height_mm;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS min_remnant_width_mm;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS standard_height_mm;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS standard_width_mm;");
        }
    }
}
