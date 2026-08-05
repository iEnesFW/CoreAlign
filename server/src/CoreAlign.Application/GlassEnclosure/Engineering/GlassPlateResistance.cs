using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Engineering;

/// <param name="ShortSpanMm">a — the SHORT side. Plate theory is written around it.</param>
/// <param name="LongSpanMm">b — the long side.</param>
/// <param name="NominalThicknessMm">Total nominal glass thickness of the pane.</param>
/// <param name="LayerThicknessesMm">
/// Individual glass plies for a laminate. Empty or single-entry means monolithic.
/// </param>
public sealed record GlassPaneGeometry(
    decimal ShortSpanMm,
    decimal LongSpanMm,
    decimal NominalThicknessMm,
    IReadOnlyList<decimal> LayerThicknessesMm);

/// <param name="CharacteristicBendingStrengthMPa">f_b;k of the finished product.</param>
/// <param name="AnnealedBaseStrengthMPa">f_g;k of annealed float — the un-prestressed part.</param>
/// <param name="IsPrestressed">Heat-strengthened / toughened glass carries a surface prestress.</param>
public sealed record GlassStrengthProfile(
    decimal CharacteristicBendingStrengthMPa,
    decimal AnnealedBaseStrengthMPa,
    bool IsPrestressed);

public sealed record GlassPlateCheck(
    decimal AppliedPressurePa,
    decimal EffectiveThicknessForStressMm,
    decimal EffectiveThicknessForDeflectionMm,
    decimal MaxBendingStressMPa,
    decimal DesignStrengthMPa,
    decimal StressUtilisation,
    decimal MaxDeflectionMm,
    decimal DeflectionLimitMm,
    decimal DeflectionUtilisation,
    decimal RequiredMonolithicThicknessMm,
    bool IsSufficient);

/// <summary>
/// Lateral-load resistance of a glass pane, sized from the pane's OWN geometry.
///
/// WHY this exists: the previous check looked up a single "allowable pressure per thickness" and
/// applied it to every pane in the project. A 4 m² fixed light and a 0.4 m² top-hung vent came back
/// with the same verdict, which is the opposite of how glass behaves — bending stress grows with
/// the SQUARE of the short span. The area column was even collected and then never read.
///
/// Two limits, both of which govern in practice:
///   strength   σ_max = β · q · a² / t²   ≤ f_g;d
///   stiffness  w_max = α · q · a⁴ / (E · t³) ≤ the serviceability limit
///
/// β and α are the classical Timoshenko coefficients for a rectangular plate simply supported on
/// four edges (ν = 0.3), interpolated on the aspect ratio. Simply-supported is the conservative
/// idealisation for glass held in a gasket or clamp — it assumes no rotational restraint at all.
///
/// The design strength follows EN 16612: an annealed part divided by γ_M;A and, for prestressed
/// products, a surface-prestress part divided by γ_M;v.
/// </summary>
public static class GlassPlateResistance
{
    /// <summary>Young's modulus of soda-lime silicate glass, EN 572-1.</summary>
    public const decimal YoungsModulusMPa = 70_000m;

    /// <summary>γ_M;A — material partial factor for the annealed part.</summary>
    public const decimal AnnealedPartialFactor = 1.8m;

    /// <summary>γ_M;v — material partial factor for the surface-prestress part.</summary>
    public const decimal PrestressPartialFactor = 1.2m;

    /// <summary>k_mod for a wind gust (short duration).</summary>
    public const decimal LoadDurationFactor = 1.0m;

    /// <summary>k_sp for float glass with an as-received surface.</summary>
    public const decimal SurfaceProfileFactor = 1.0m;

    /// <summary>k_e — edge strength factor for a pane loaded on its face.</summary>
    public const decimal EdgeFactor = 1.0m;

    /// <summary>k_v — strengthening factor for horizontally toughened glass.</summary>
    public const decimal StrengtheningFactor = 1.0m;

    /// <summary>Deflection limit for a framed facade pane: the shorter of span/60 and 50 mm.</summary>
    public const decimal AbsoluteDeflectionLimitMm = 50m;
    public const decimal SpanDeflectionDivisor = 60m;

    // Timoshenko, "Theory of Plates and Shells", uniformly loaded rectangular plate on four simple
    // supports, ν = 0.3. β scales the maximum bending moment, α the centre deflection. Both flatten
    // out past b/a ≈ 5 into the one-way strip solution.
    private static readonly (decimal Ratio, decimal Beta, decimal Alpha)[] PlateCoefficients =
    {
        (1.0m, 0.2874m, 0.04062m),
        (1.2m, 0.3762m, 0.05650m),
        (1.4m, 0.4530m, 0.07000m),
        (1.6m, 0.5172m, 0.08120m),
        (1.8m, 0.5688m, 0.08880m),
        (2.0m, 0.6102m, 0.09460m),
        (3.0m, 0.7134m, 0.11170m),
        (4.0m, 0.7410m, 0.11350m),
        (5.0m, 0.7476m, 0.11400m),
        (1000m, 0.7500m, 0.11420m),
    };

    /// <summary>
    /// f_b;k by product family. EN 572-1 gives 45 MPa for annealed float; EN 1863 and EN 12150 give
    /// 70 and 120 MPa for heat-strengthened and thermally toughened soda-lime glass.
    /// </summary>
    public static GlassStrengthProfile StrengthFor(GlassStructure structure) => structure switch
    {
        GlassStructure.Tempered => new GlassStrengthProfile(120m, 45m, true),
        // A laminate's plies are ordinarily annealed unless the catalogue says otherwise; treating
        // it as annealed is the conservative reading.
        GlassStructure.Laminated => new GlassStrengthProfile(45m, 45m, false),
        _ => new GlassStrengthProfile(45m, 45m, false),
    };

    /// <summary>EN 16612 §7: design strength, annealed part plus prestress part.</summary>
    public static decimal DesignStrengthMPa(GlassStrengthProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var annealed =
            EdgeFactor * LoadDurationFactor * SurfaceProfileFactor * profile.AnnealedBaseStrengthMPa
            / AnnealedPartialFactor;
        if (!profile.IsPrestressed) return decimal.Round(annealed, 2);

        var prestress =
            StrengtheningFactor
            * Math.Max(0m, profile.CharacteristicBendingStrengthMPa - profile.AnnealedBaseStrengthMPa)
            / PrestressPartialFactor;
        return decimal.Round(annealed + prestress, 2);
    }

    /// <summary>
    /// Effective thickness of a laminate with NO shear transfer through the interlayer (ω = 0) —
    /// the conservative bound EN 16612 falls back to when the interlayer's shear modulus at the
    /// design temperature and load duration is not established. A monolithic pane returns itself.
    /// </summary>
    public static (decimal ForStress, decimal ForDeflection) EffectiveThicknessMm(
        GlassPaneGeometry pane)
    {
        ArgumentNullException.ThrowIfNull(pane);
        var plies = (pane.LayerThicknessesMm ?? Array.Empty<decimal>())
            .Where(t => t > 0m)
            .ToList();
        if (plies.Count <= 1)
        {
            var t = plies.Count == 1 ? plies[0] : pane.NominalThicknessMm;
            var mono = decimal.Round(Math.Max(0.1m, t), 3);
            return (mono, mono);
        }

        // t_ef;w = cube root of the sum of the cubes; with ω = 0 the stress thickness matches it.
        var sumCubes = plies.Sum(t => t * t * t);
        var tef = (decimal)Math.Cbrt((double)sumCubes);
        var rounded = decimal.Round(Math.Max(0.1m, tef), 3);
        return (rounded, rounded);
    }

    public static GlassPlateCheck Check(
        GlassPaneGeometry pane,
        decimal appliedPressurePa,
        GlassStrengthProfile strength)
    {
        ArgumentNullException.ThrowIfNull(pane);
        ArgumentNullException.ThrowIfNull(strength);

        var a = Math.Max(1m, Math.Min(pane.ShortSpanMm, pane.LongSpanMm));
        var b = Math.Max(a, Math.Max(pane.ShortSpanMm, pane.LongSpanMm));
        var (beta, alpha) = CoefficientsFor(b / a);

        var (tStress, tDeflection) = EffectiveThicknessMm(pane);
        // Pa -> MPa (N/mm²) so every length can stay in mm.
        var q = Math.Max(0m, appliedPressurePa) / 1_000_000m;

        var sigma = beta * q * a * a / (tStress * tStress);
        var design = DesignStrengthMPa(strength);
        var deflection = alpha * q * a * a * a * a / (YoungsModulusMPa * tDeflection * tDeflection * tDeflection);
        var deflectionLimit = Math.Min(AbsoluteDeflectionLimitMm, a / SpanDeflectionDivisor);

        // Invert the strength check for the thickness a monolithic pane would need. Deflection can
        // still govern above it, which is why both utilisations are reported.
        var required = design > 0m
            ? a * (decimal)Math.Sqrt((double)(beta * q / design))
            : 0m;

        var stressUtil = design > 0m ? sigma / design : 0m;
        var deflectionUtil = deflectionLimit > 0m ? deflection / deflectionLimit : 0m;

        return new GlassPlateCheck(
            decimal.Round(appliedPressurePa, 1),
            tStress,
            tDeflection,
            decimal.Round(sigma, 3),
            design,
            decimal.Round(stressUtil, 4),
            decimal.Round(deflection, 2),
            decimal.Round(deflectionLimit, 2),
            decimal.Round(deflectionUtil, 4),
            decimal.Round(required, 2),
            stressUtil <= 1m && deflectionUtil <= 1m);
    }

    private static (decimal Beta, decimal Alpha) CoefficientsFor(decimal aspectRatio)
    {
        var ratio = Math.Max(1m, aspectRatio);
        for (var i = 0; i < PlateCoefficients.Length; i += 1)
        {
            var current = PlateCoefficients[i];
            if (ratio <= current.Ratio)
            {
                if (i == 0) return (current.Beta, current.Alpha);
                var previous = PlateCoefficients[i - 1];
                var span = current.Ratio - previous.Ratio;
                var f = span == 0m ? 0m : (ratio - previous.Ratio) / span;
                return (
                    previous.Beta + f * (current.Beta - previous.Beta),
                    previous.Alpha + f * (current.Alpha - previous.Alpha));
            }
        }
        var last = PlateCoefficients[^1];
        return (last.Beta, last.Alpha);
    }
}
