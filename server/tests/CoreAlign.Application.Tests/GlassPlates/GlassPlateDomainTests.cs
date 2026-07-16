using CoreAlign.Domain.Entities.GlassPlates;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.GlassPlates;

public class GlassPlateDomainTests
{
    private static GlassPlate NewPlate(decimal w = 2000m, decimal h = 1000m, decimal thickness = 4m) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  PL-001  ",
            w,
            h,
            thickness,
            PlateKind.Fresh,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Unspecified));

    [Fact]
    public void Constructor_sets_area_status_and_normalizes()
    {
        var plate = NewPlate();

        plate.PlateNumber.Should().Be("PL-001");
        plate.Kind.Should().Be(PlateKind.Fresh);
        plate.Status.Should().Be(GlassPlateStatus.Available);
        plate.OriginalAreaMm2.Should().Be(2_000m * 1_000m);
        plate.RemainingAreaMm2.Should().Be(plate.OriginalAreaMm2);
        plate.ReceivedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(0, 1000, 4)]
    [InlineData(-1, 1000, 4)]
    [InlineData(2000, 0, 4)]
    [InlineData(2000, -5, 4)]
    [InlineData(2000, 1000, -1)]
    public void Constructor_rejects_non_positive_dimensions(decimal w, decimal h, decimal thickness)
    {
        var act = () => NewPlate(w, h, thickness);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConsumeArea_reduces_remaining()
    {
        var plate = NewPlate();

        plate.ConsumeArea(500_000m, DateTime.UtcNow);

        plate.RemainingAreaMm2.Should().Be(2_000_000m - 500_000m);
        plate.Status.Should().Be(GlassPlateStatus.Available);
    }

    [Fact]
    public void ConsumeArea_rejects_non_positive()
    {
        var plate = NewPlate();
        var act = () => plate.ConsumeArea(0m, DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConsumeArea_rejects_exceeding_remaining()
    {
        var plate = NewPlate();
        var act = () => plate.ConsumeArea(plate.RemainingAreaMm2 + 100m, DateTime.UtcNow);
        act.Should().Throw<GlassPlateAreaExceededException>();
    }

    [Fact]
    public void ConsumeArea_allows_full_area_within_epsilon()
    {
        var plate = NewPlate();
        var act = () => plate.ConsumeArea(plate.RemainingAreaMm2 + 0.5m, DateTime.UtcNow);

        act.Should().NotThrow();
        plate.RemainingAreaMm2.Should().Be(0m);
    }

    [Fact]
    public void ConsumeArea_rejected_after_scrap()
    {
        var plate = NewPlate();
        plate.Scrap(DateTime.UtcNow);

        var act = () => plate.ConsumeArea(100m, DateTime.UtcNow);
        act.Should().Throw<InvalidGlassPlateTransitionException>();
    }

    [Fact]
    public void CreateRemnant_inherits_thickness_and_links_parent()
    {
        var plate = NewPlate(thickness: 6m);

        var remnant = plate.CreateRemnant("RM-1", 500m, 400m, DateTime.UtcNow);

        remnant.Kind.Should().Be(PlateKind.Remnant);
        remnant.ThicknessMm.Should().Be(6m);
        remnant.ParentPlateId.Should().Be(plate.Id);
        remnant.OriginalAreaMm2.Should().Be(500m * 400m);
        remnant.Status.Should().Be(GlassPlateStatus.Available);
    }

    [Fact]
    public void Reserve_then_release_returns_to_available()
    {
        var plate = NewPlate();
        var jobId = Guid.NewGuid();

        plate.Reserve(jobId);
        plate.Status.Should().Be(GlassPlateStatus.Reserved);
        plate.ReservedByJobId.Should().Be(jobId);

        plate.Release();
        plate.Status.Should().Be(GlassPlateStatus.Available);
        plate.ReservedByJobId.Should().BeNull();
    }

    [Fact]
    public void Release_rejected_when_not_reserved()
    {
        var plate = NewPlate();
        var act = () => plate.Release();
        act.Should().Throw<InvalidGlassPlateTransitionException>();
    }

    [Fact]
    public void Scrap_marks_terminal_and_sets_consumed_at_utc()
    {
        var plate = NewPlate();

        plate.Scrap(new DateTime(2026, 7, 2, 8, 0, 0, DateTimeKind.Unspecified));

        plate.Status.Should().Be(GlassPlateStatus.Scrapped);
        plate.ConsumedAtUtc.Should().NotBeNull();
        plate.ConsumedAtUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void MarkConsumed_from_available_is_allowed()
    {
        var plate = NewPlate();
        plate.MarkConsumed(DateTime.UtcNow);
        plate.Status.Should().Be(GlassPlateStatus.Consumed);
    }

    [Fact]
    public void Terminal_plate_rejects_further_transitions()
    {
        var plate = NewPlate();
        plate.MarkConsumed(DateTime.UtcNow);

        var reserve = () => plate.Reserve(Guid.NewGuid());
        var scrap = () => plate.Scrap(DateTime.UtcNow);

        reserve.Should().Throw<InvalidGlassPlateTransitionException>();
        scrap.Should().Throw<InvalidGlassPlateTransitionException>();
    }

    [Fact]
    public void MoveTo_updates_warehouse_and_location()
    {
        var plate = NewPlate();
        var newWarehouse = Guid.NewGuid();
        var newLocation = Guid.NewGuid();

        plate.MoveTo(newWarehouse, newLocation);

        plate.WarehouseId.Should().Be(newWarehouse);
        plate.StorageLocationId.Should().Be(newLocation);
    }
}
