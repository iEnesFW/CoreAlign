using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Phase140ShortUnitCodes : Migration
{
    // Reference-data rename, not a schema change: the curated unit codes move from long Turkish
    // words (KILOGRAM, METREKARE) to the short codes an operator actually types (KG, M2), matching
    // how Turkish ERPs label a unit card. The UBL-TR translation stays in GibUnitCodeMap, so the
    // stored code is a human label and the e-invoice code is derived — the two never merge.
    //
    // SeedStandardUnitsOfMeasureHandler only INSERTS codes it cannot find, so without this rename an
    // already-seeded tenant would end up with 74 rows (37 old + 37 new) instead of 37 renamed ones.
    private static readonly (string Old, string New)[] UnitRenames =
    {
        ("DUZINE", "DZ"), ("PAKET", "PK"), ("ROLE", "RULO"),
        ("METRE", "MT"), ("MILIMETRE", "MM"), ("SANTIMETRE", "CM"), ("DESIMETRE", "DM"),
        ("KILOMETRE", "KM"), ("YARDA", "YD"),
        ("KILOGRAM", "KG"), ("GRAM", "GR"), ("MILIGRAM", "MG"), ("LIBRE", "LB"),
        ("LITRE", "LT"), ("MILILITRE", "ML"), ("SANTILITRE", "CL"), ("METREKUP", "M3"),
        ("METREKARE", "M2"), ("SANTIMETREKARE", "CM2"), ("KILOMETREKARE", "KM2"),
        ("HEKTAR", "HA"), ("DEKAR", "DA"),
        ("DAKIKA", "DK"), ("SANIYE", "SN"),
    };

    // Free-text unit columns that predate the curated list. Values measured in the live database
    // plus the long codes this migration retires, so a row written either way lands on one alphabet.
    private static readonly (string Old, string New)[] FreeTextRenames =
    {
        ("Kg", "KG"), ("kg", "KG"), ("KILOGRAM", "KG"),
        ("pcs", "ADET"), ("PCS", "ADET"), ("Piece", "ADET"), ("piece", "ADET"), ("adet", "ADET"),
        ("M2", "M2"), ("m2", "M2"), ("m²", "M2"), ("METREKARE", "M2"),
        ("Meter", "MT"), ("meter", "MT"), ("m", "MT"), ("METRE", "MT"),
        ("LITRE", "LT"), ("TON", "TON"), ("SAAT", "SAAT"),
        ("hour", "SAAT"), ("GRAM", "GR"), ("METREKUP", "M3"),
    };

    private static readonly string[] LineTables =
    {
        "order_lines", "invoice_lines", "quote_lines", "purchase_order_lines",
        "vendor_bill_lines", "return_request_lines", "recurring_invoice_template_lines",
    };

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var (oldCode, newCode) in UnitRenames)
        {
            // Guarded so the rename is re-runnable and can never collide with a code a tenant
            // already created by hand.
            migrationBuilder.Sql($@"
UPDATE units_of_measure u
SET code = '{newCode}'
WHERE u.code = '{oldCode}'
  AND NOT EXISTS (
    SELECT 1 FROM units_of_measure x
    WHERE x.tenant_id = u.tenant_id AND upper(x.code) = '{newCode}');");
        }

        foreach (var (oldValue, newValue) in FreeTextRenames)
        {
            if (oldValue == newValue) continue;
            migrationBuilder.Sql(
                $"UPDATE products SET unit = '{newValue}' WHERE unit = '{oldValue}';");
            migrationBuilder.Sql(
                $"UPDATE production_jobs SET unit_of_measure = '{newValue}' WHERE unit_of_measure = '{oldValue}';");
            migrationBuilder.Sql(
                $"UPDATE glass_hardware_items SET unit = '{newValue}' WHERE unit = '{oldValue}';");
            foreach (var table in LineTables)
            {
                migrationBuilder.Sql(
                    $"UPDATE {table} SET uom_code = '{newValue}' WHERE uom_code = '{oldValue}';");
            }
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var (oldCode, newCode) in UnitRenames)
        {
            migrationBuilder.Sql($@"
UPDATE units_of_measure u
SET code = '{oldCode}'
WHERE u.code = '{newCode}'
  AND NOT EXISTS (
    SELECT 1 FROM units_of_measure x
    WHERE x.tenant_id = u.tenant_id AND upper(x.code) = '{oldCode}');");
        }
    }
}
