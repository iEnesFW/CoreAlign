using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase118ShipmentEDespatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE shipments ADD COLUMN IF NOT EXISTS carrier_vkn character varying(11) NULL;");
            migrationBuilder.Sql("ALTER TABLE shipments ADD COLUMN IF NOT EXISTS vehicle_plate character varying(20) NULL;");
            migrationBuilder.Sql("ALTER TABLE shipments ADD COLUMN IF NOT EXISTS driver_name character varying(150) NULL;");
            migrationBuilder.Sql("ALTER TABLE shipments ADD COLUMN IF NOT EXISTS driver_tckn character varying(11) NULL;");
            migrationBuilder.Sql("ALTER TABLE shipments ADD COLUMN IF NOT EXISTS e_despatch_uuid character varying(64) NULL;");
            migrationBuilder.Sql("ALTER TABLE shipments ADD COLUMN IF NOT EXISTS e_despatch_status character varying(20) NULL;");
            migrationBuilder.Sql("ALTER TABLE shipments ADD COLUMN IF NOT EXISTS e_despatch_profile character varying(32) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE shipments DROP COLUMN IF EXISTS carrier_vkn;");
            migrationBuilder.Sql("ALTER TABLE shipments DROP COLUMN IF EXISTS vehicle_plate;");
            migrationBuilder.Sql("ALTER TABLE shipments DROP COLUMN IF EXISTS driver_name;");
            migrationBuilder.Sql("ALTER TABLE shipments DROP COLUMN IF EXISTS driver_tckn;");
            migrationBuilder.Sql("ALTER TABLE shipments DROP COLUMN IF EXISTS e_despatch_uuid;");
            migrationBuilder.Sql("ALTER TABLE shipments DROP COLUMN IF EXISTS e_despatch_status;");
            migrationBuilder.Sql("ALTER TABLE shipments DROP COLUMN IF EXISTS e_despatch_profile;");
        }
    }
}
