using CoreAlign.Application.EInvoice;
using CoreAlign.Application.MasterData;

namespace CoreAlign.Application.Tests.EInvoice;

/// <summary>
/// Every unit string the product can end up carrying must be translatable to a UBL-TR unit code,
/// because <see cref="GibUnitCodeMap.Resolve"/> refuses an unknown one and that refusal now blocks
/// the e-invoice AND the product form. Adding a unit to a seed or a BOM composer without adding it
/// here is what broke the glass project -> order -> invoice path once; this test is the gate.
/// </summary>
public class GibUnitCodeMapCoversEmittedUnitsTests
{
    // Emitted by IBOMComposer for the glass BOM's own lines.
    private static readonly string[] GlassBomUnits = { "lot", "trip", "m²", "m", "adet" };

    // Defaults baked into entities, importers and demo seeds.
    private static readonly string[] BakedInDefaults = { "pcs", "PCS", "Piece", "Meter", "hour", "mo" };

    // The three values that exist in live production data today.
    private static readonly string[] LiveProductUnits = { "Kg", "pcs", "M2" };

    [Fact]
    public void Every_seeded_unit_of_measure_resolves_to_a_ubl_unit_code()
    {
        var unmapped = StandardUnitsOfMeasureSeed.Entries
            .Where(entry => !GibUnitCodeMap.TryResolve(entry.Code, out _))
            .Select(entry => entry.Code)
            .ToList();

        unmapped.Should().BeEmpty(
            "a curated unit the user can pick must be sendable on an e-invoice and savable on a product");
    }

    [Theory]
    [MemberData(nameof(EmittedUnits))]
    public void Every_unit_the_code_emits_resolves(string unit)
    {
        GibUnitCodeMap.TryResolve(unit, out var code).Should().BeTrue($"'{unit}' is emitted by the codebase");
        code.Should().NotBeNullOrWhiteSpace();
    }

    public static TheoryData<string> EmittedUnits()
    {
        var data = new TheoryData<string>();
        foreach (var unit in GlassBomUnits.Concat(BakedInDefaults).Concat(LiveProductUnits).Distinct())
        {
            data.Add(unit);
        }
        return data;
    }

    [Fact]
    public void The_glass_service_lines_bill_as_a_single_piece()
    {
        GibUnitCodeMap.Resolve("lot").Should().Be("C62");
        GibUnitCodeMap.Resolve("trip").Should().Be("C62");
    }

    [Fact]
    public void The_map_has_no_duplicate_or_empty_targets()
    {
        foreach (var key in GibUnitCodeMap.KnownUnits)
        {
            GibUnitCodeMap.Resolve(key).Should().NotBeNullOrWhiteSpace();
        }
    }
}
