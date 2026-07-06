namespace CoreAlign.Domain.Entities;

// Area maths for order lines whose unit of measure is a square unit (m², cm², dm², mm²). Such lines
// are stocked / priced by area but entered as a cut size (width × height in millimetres) times a
// piece count. This is the single source of truth for that conversion, mirrored client-side in the
// order-line editor. Non-area units (mt, kg, adet …) resolve to null and keep their plain quantity.
public static class GlassLineMath
{
    // For a square unit, how many square millimetres make up one unit of it. Width/height are always
    // entered in millimetres, so dividing the raw mm² area by this yields the quantity in that unit.
    public static decimal? AreaUnitDivisor(string? unitCode)
    {
        var code = unitCode?.Trim().ToLowerInvariant().Replace("²", "2");
        return code switch
        {
            "m2" or "sqm" or "metrekare" => 1_000_000m,
            "dm2" => 10_000m,
            "cm2" => 100m,
            "mm2" => 1m,
            _ => null,
        };
    }

    public static bool IsAreaUnit(string? unitCode) => AreaUnitDivisor(unitCode) is not null;

    // Total area in the unit's own square measure from a cut size (width × height in mm) × pieces.
    // Returns null when the unit is not a square unit or any dimension is missing / non-positive.
    public static decimal? Area(string? unitCode, decimal? widthMm, decimal? heightMm, decimal? pieces)
    {
        var divisor = AreaUnitDivisor(unitCode);
        if (divisor is null || widthMm is not > 0m || heightMm is not > 0m || pieces is not > 0m)
        {
            return null;
        }

        return decimal.Round(pieces.Value * widthMm.Value * heightMm.Value / divisor.Value, 4);
    }
}
