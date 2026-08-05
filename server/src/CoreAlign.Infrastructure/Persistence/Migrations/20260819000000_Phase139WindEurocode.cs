using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase139WindEurocode : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE wind_zones
                ADD COLUMN IF NOT EXISTS basic_wind_speed_ms numeric(6,2) NOT NULL DEFAULT 0;
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE glass_projects
                ADD COLUMN IF NOT EXISTS wind_terrain_category integer NOT NULL DEFAULT 2;
            """);

        // TS EN 1991-1-4 Table 4.1 defines exactly five roughness bands (0-IV); anything else would
        // silently fall back to terrain II inside the calculator instead of being rejected here.
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'ck_glass_projects_wind_terrain_category'
                ) THEN
                    ALTER TABLE glass_projects
                        ADD CONSTRAINT ck_glass_projects_wind_terrain_category
                        CHECK (wind_terrain_category BETWEEN 0 AND 4) NOT VALID;
                END IF;
            END $$;
            """);

        // Recover a basic wind speed for zones that only ever stored a pressure, by inverting
        // q_b = 0.5*rho*v^2 with rho = 1.25. The result is no better than the pressure it came
        // from, which is exactly what those rows already claimed — it just makes the Eurocode
        // chain runnable until the zone is resurveyed against the national wind map.
        migrationBuilder.Sql(
            """
            UPDATE wind_zones
               SET basic_wind_speed_ms = ROUND(SQRT(base_wind_pressure_pa * 2.0 / 1.25)::numeric, 2)
             WHERE basic_wind_speed_ms = 0
               AND base_wind_pressure_pa > 0;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "ALTER TABLE glass_projects DROP CONSTRAINT IF EXISTS ck_glass_projects_wind_terrain_category;");
        migrationBuilder.Sql(
            "ALTER TABLE glass_projects DROP COLUMN IF EXISTS wind_terrain_category;");
        migrationBuilder.Sql("ALTER TABLE wind_zones DROP COLUMN IF EXISTS basic_wind_speed_ms;");
    }
}
