namespace CoreAlign.Application.GlassEnclosure.Engineering;

/// <summary>
/// Terrain roughness bands of TS EN 1991-1-4 Table 4.1. Each carries the roughness length z0 and
/// the minimum height z_min the logarithmic profile is valid from.
/// </summary>
public enum WindTerrainCategory
{
    /// <summary>0 — sea, coastal area exposed to the open sea.</summary>
    Sea = 0,

    /// <summary>I — lakes or flat, featureless country with negligible vegetation.</summary>
    OpenFlat = 1,

    /// <summary>II — low vegetation and isolated obstacles at least 20 obstacle heights apart.</summary>
    Country = 2,

    /// <summary>III — regular vegetation, suburbs, permanent forest.</summary>
    Suburban = 3,

    /// <summary>IV — at least 15 % of the surface covered by buildings over 15 m tall.</summary>
    Urban = 4,
}

public sealed record TerrainProfile(decimal RoughnessLengthM, decimal MinimumHeightM);

/// <param name="ExternalWindwardCpe">c_pe on the pressure (windward) face.</param>
/// <param name="ExternalSuctionCpe">Most negative c_pe over the element's zone (suction).</param>
/// <param name="InternalPositiveCpi">c_pi that makes the windward case worst.</param>
/// <param name="InternalNegativeCpi">c_pi that makes the suction case worst.</param>
public sealed record WindPressureCoefficients(
    decimal ExternalWindwardCpe,
    decimal ExternalSuctionCpe,
    decimal InternalPositiveCpi,
    decimal InternalNegativeCpi)
{
    /// <summary>
    /// EN 1991-1-4 Table 7.1 (vertical walls, c_pe,10) with the §7.2.9 internal-pressure pair for
    /// a building whose opening ratio is not established. Zone A suction governs a glazed
    /// enclosure far more often than the windward face does.
    /// </summary>
    public static readonly WindPressureCoefficients EnclosureDefault = new(0.8m, -1.2m, 0.2m, -0.3m);
}

/// <param name="BasicWindSpeedMs">v_b,0 — the 10 min mean at 10 m over terrain II, from the map.</param>
/// <param name="DirectionFactor">c_dir. 1.0 unless a directional study says otherwise.</param>
/// <param name="SeasonFactor">c_season. 1.0 for a permanent structure.</param>
/// <param name="OrographyFactor">c_o. 1.0 on flat ground; &gt;1 on a hill or escarpment.</param>
/// <param name="AirDensityKgM3">ρ. 1.25 at sea level and 20 °C.</param>
public sealed record WindSiteInput(
    decimal BasicWindSpeedMs,
    WindTerrainCategory Terrain,
    decimal ReferenceHeightM,
    decimal DirectionFactor = 1m,
    decimal SeasonFactor = 1m,
    decimal OrographyFactor = 1m,
    decimal AirDensityKgM3 = 1.25m);

/// <summary>Every intermediate value, so the report can be audited rather than trusted.</summary>
public sealed record WindSitePressure(
    decimal BasicWindSpeedMs,
    decimal DesignWindSpeedMs,
    decimal BasicVelocityPressurePa,
    decimal ReferenceHeightM,
    decimal RoughnessFactor,
    decimal MeanWindSpeedMs,
    decimal TurbulenceIntensity,
    decimal PeakVelocityPressurePa);

/// <summary>
/// The TS EN 1991-1-4 peak velocity pressure chain.
///
/// WHY this replaced the previous model: that one multiplied a stored "base pressure" by
/// <c>1 + height/100</c>. It had no terrain, no turbulence and no pressure coefficients, so the
/// same number came out for a ground-floor balcony in a city centre and a 40 m coastal facade —
/// and nothing in it could be traced back to a clause an engineer could sign off.
///
/// Every coefficient is an input with a documented default; none is hidden in the arithmetic.
/// </summary>
public static class EurocodeWindPressure
{
    /// <summary>Table 4.1. Terrain II is the reference the roughness factor is normalised against.</summary>
    public static TerrainProfile ProfileFor(WindTerrainCategory terrain) => terrain switch
    {
        WindTerrainCategory.Sea => new TerrainProfile(0.003m, 1m),
        WindTerrainCategory.OpenFlat => new TerrainProfile(0.01m, 1m),
        WindTerrainCategory.Country => new TerrainProfile(0.05m, 2m),
        WindTerrainCategory.Suburban => new TerrainProfile(0.3m, 5m),
        WindTerrainCategory.Urban => new TerrainProfile(1.0m, 10m),
        _ => new TerrainProfile(0.05m, 2m),
    };

    private static readonly TerrainProfile ReferenceTerrain = ProfileFor(WindTerrainCategory.Country);

    /// <summary>k_I, the turbulence factor. 1.0 is the recommended value.</summary>
    private const decimal TurbulenceFactor = 1m;

    public static WindSitePressure Compute(WindSiteInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.BasicWindSpeedMs <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input),
                "A basic wind speed is required to derive a peak velocity pressure.");
        }

        var profile = ProfileFor(input.Terrain);
        var rho = input.AirDensityKgM3 > 0m ? input.AirDensityKgM3 : 1.25m;
        var co = input.OrographyFactor > 0m ? input.OrographyFactor : 1m;

        // §4.2(2)P — the site's basic wind velocity.
        var vb = input.DirectionFactor * input.SeasonFactor * input.BasicWindSpeedMs;
        var qb = 0.5m * rho * vb * vb;

        // §4.3.2 — the log profile is only defined from z_min upward.
        var z = Math.Max(profile.MinimumHeightM, Math.Max(0m, input.ReferenceHeightM));

        // k_r = 0.19 · (z0 / z0,II)^0.07
        var kr = 0.19m * Pow(profile.RoughnessLengthM / ReferenceTerrain.RoughnessLengthM, 0.07d);
        var cr = kr * (decimal)Math.Log((double)(z / profile.RoughnessLengthM));
        var vm = cr * co * vb;

        // §4.4 — turbulence intensity, and §4.5 the peak velocity pressure it produces.
        var iv = TurbulenceFactor / (co * (decimal)Math.Log((double)(z / profile.RoughnessLengthM)));
        var qp = (1m + 7m * iv) * 0.5m * rho * vm * vm;

        return new WindSitePressure(
            decimal.Round(input.BasicWindSpeedMs, 2),
            decimal.Round(vb, 2),
            decimal.Round(qb, 1),
            decimal.Round(z, 2),
            decimal.Round(cr, 4),
            decimal.Round(vm, 2),
            decimal.Round(iv, 4),
            decimal.Round(qp, 1));
    }

    /// <summary>
    /// The governing NET pressure magnitude on a facade element: the worse of "wind pushing in
    /// while the inside sucks" and "wind sucking out while the inside pushes". Glass has to survive
    /// both, so the check uses whichever is larger.
    /// </summary>
    public static decimal GoverningNetPressurePa(
        decimal peakVelocityPressurePa,
        WindPressureCoefficients coefficients)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        var pressureCase = Math.Abs(coefficients.ExternalWindwardCpe - coefficients.InternalNegativeCpi);
        var suctionCase = Math.Abs(coefficients.ExternalSuctionCpe - coefficients.InternalPositiveCpi);
        return decimal.Round(peakVelocityPressurePa * Math.Max(pressureCase, suctionCase), 1);
    }

    private static decimal Pow(decimal value, double exponent) =>
        (decimal)Math.Pow((double)value, exponent);
}
