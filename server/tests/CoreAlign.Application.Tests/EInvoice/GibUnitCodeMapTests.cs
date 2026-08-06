using CoreAlign.Application.EInvoice;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.EInvoice;

public class GibUnitCodeMapTests
{
    [Theory]
    [InlineData("ADET", "C62")]
    [InlineData("pcs", "C62")]
    [InlineData("Kg", "KGM")]
    [InlineData("KILOGRAM", "KGM")]
    [InlineData("M2", "MTK")]
    [InlineData("m²", "MTK")]
    [InlineData("METREKARE", "MTK")]
    [InlineData("SANTIMETREKARE", "CMK")]
    [InlineData("METRE", "MTR")]
    [InlineData("LITRE", "LTR")]
    [InlineData("TON", "TNE")]
    [InlineData("KOLI", "CT")]
    [InlineData("CIFT", "PR")]
    [InlineData("SAAT", "HUR")]
    public void Stored_units_resolve_to_their_un_ece_code(string stored, string expected)
    {
        GibUnitCodeMap.Resolve(stored).Should().Be(expected);
    }

    [Theory]
    [InlineData("C62")]
    [InlineData("KGM")]
    [InlineData("MTK")]
    public void Resolving_an_already_mapped_code_is_idempotent(string code)
    {
        GibUnitCodeMap.Resolve(code).Should().Be(code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_unit_falls_back_to_piece(string? unit)
    {
        GibUnitCodeMap.Resolve(unit).Should().Be(GibUnitCodeMap.DefaultCode);
    }

    // WHY this is the point of the class: quietly emitting C62 for an unknown unit would bill a
    // kilogram as a piece on a legally binding document.
    [Theory]
    [InlineData("Kilo")]
    [InlineData("beher")]
    [InlineData("m^2")]
    public void An_unmapped_unit_stops_the_document_instead_of_defaulting(string unit)
    {
        var act = () => GibUnitCodeMap.Resolve(unit);

        act.Should().Throw<UnmappedUnitCodeException>().WithMessage($"*{unit}*");
    }

    [Fact]
    public void Every_stored_product_unit_in_the_live_shape_is_mappable()
    {
        foreach (var unit in new[] { "Kg", "pcs", "M2" })
        {
            GibUnitCodeMap.TryResolve(unit, out _).Should().BeTrue($"'{unit}' exists in production data");
        }
    }

    // The curated units_of_measure codes and the GIB codes must BOTH keep glass area derivation
    // working; a unit the map accepts but the area maths does not would silently turn m² into pieces.
    [Theory]
    [InlineData("METREKARE", 1_000_000)]
    [InlineData("MTK", 1_000_000)]
    [InlineData("SANTIMETREKARE", 100)]
    [InlineData("CMK", 100)]
    [InlineData("MILIMETREKARE", 1)]
    [InlineData("MMK", 1)]
    [InlineData("DESIMETREKARE", 10_000)]
    [InlineData("DMK", 10_000)]
    public void Curated_and_gib_area_units_still_derive_area(string unit, int divisor)
    {
        GlassLineMath.AreaUnitDivisor(unit).Should().Be(divisor);
    }

    [Theory]
    [InlineData("KILOGRAM")]
    [InlineData("ADET")]
    [InlineData("KGM")]
    [InlineData("C62")]
    public void Non_area_units_keep_their_plain_quantity(string unit)
    {
        GlassLineMath.AreaUnitDivisor(unit).Should().BeNull();
    }
}
