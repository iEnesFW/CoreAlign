using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests.Orders;

// Order lines with a square unit of measure are entered as a cut size (width × height mm) × piece
// count and priced / costed / stocked by the derived area IN THAT UNIT. These lock the conversion,
// that dimensions override the base quantity for a square unit, and that a non-area unit (or a line
// with missing dimensions) is untouched.
public class GlassLineMathTests
{
    [Fact]
    public void Area_computes_pieces_times_width_times_height_per_unit()
    {
        GlassLineMath.Area("m2", 1000m, 2000m, 3m).Should().Be(6m);       // 3 × 1m × 2m
        GlassLineMath.Area("m2", 1200m, 800m, 1m).Should().Be(0.96m);     // 1.2m × 0.8m
        GlassLineMath.Area("cm2", 100m, 200m, 1m).Should().Be(200m);      // 10cm × 20cm
        GlassLineMath.Area("mm2", 100m, 200m, 1m).Should().Be(20000m);
    }

    [Fact]
    public void Area_is_null_for_non_area_units()
    {
        GlassLineMath.Area("adet", 1000m, 2000m, 3m).Should().BeNull();
        GlassLineMath.Area("mt", 1000m, 2000m, 3m).Should().BeNull();
        GlassLineMath.Area(null, 1000m, 2000m, 3m).Should().BeNull();
    }

    [Fact]
    public void Area_is_null_when_any_dimension_missing_or_nonpositive()
    {
        GlassLineMath.Area("m2", null, 2000m, 3m).Should().BeNull();
        GlassLineMath.Area("m2", 1000m, 0m, 3m).Should().BeNull();
        GlassLineMath.Area("m2", 1000m, 2000m, null).Should().BeNull();
    }

    [Fact]
    public void IsAreaUnit_recognizes_square_unit_codes_case_and_symbol_insensitively()
    {
        GlassLineMath.IsAreaUnit("m2").Should().BeTrue();
        GlassLineMath.IsAreaUnit("M²").Should().BeTrue();
        GlassLineMath.IsAreaUnit("cm2").Should().BeTrue();
        GlassLineMath.IsAreaUnit("adet").Should().BeFalse();
        GlassLineMath.IsAreaUnit(null).Should().BeFalse();
    }

    [Fact]
    public void SetGlassDimensions_derives_line_quantity_as_total_area_and_reprices()
    {
        var line = new OrderLine(Guid.NewGuid(), "SKU-4MM", "4mm cam", quantity: 1m, unitPrice: 100m);

        line.SetGlassDimensions(1000m, 2000m, 3m, "m2");

        line.WidthMm.Should().Be(1000m);
        line.HeightMm.Should().Be(2000m);
        line.Pieces.Should().Be(3m);
        line.Quantity.Should().Be(6m);
        line.LineSubtotal.Should().Be(600m);
    }

    [Fact]
    public void SetGlassDimensions_on_non_area_unit_leaves_quantity_unchanged()
    {
        var line = new OrderLine(Guid.NewGuid(), "SKU", "Widget", quantity: 5m, unitPrice: 10m);

        line.SetGlassDimensions(1000m, 2000m, 3m, "adet");

        line.Quantity.Should().Be(5m);
        line.WidthMm.Should().Be(1000m);
    }

    [Fact]
    public void SetGlassDimensions_with_nulls_leaves_quantity_unchanged()
    {
        var line = new OrderLine(Guid.NewGuid(), "SKU", "Widget", quantity: 5m, unitPrice: 10m);

        line.SetGlassDimensions(null, null, null, "m2");

        line.Quantity.Should().Be(5m);
        line.WidthMm.Should().BeNull();
    }
}
