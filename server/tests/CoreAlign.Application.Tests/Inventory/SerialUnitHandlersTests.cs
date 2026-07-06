using CoreAlign.Application.Inventory.Serials;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Inventory;

// Serial genealogy vertical: register captures per-unit serials for a serial-tracked product
// (duplicates rejected), ship stamps the where-used links + status FSM, and where-used returns the
// unit plus its production genealogy (component children).
public class SerialUnitHandlersTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ISerialUnitRepository _serials = Substitute.For<ISerialUnitRepository>();

    private static Product SerialTrackedProduct()
    {
        var p = new Product("SKU-S", "Serialized", "pcs", 10m, "TRY") { Id = ProductId };
        p.SetSerialTracked(true);
        return p;
    }

    [Fact]
    public async Task Register_creates_serial_units_for_a_serial_tracked_product()
    {
        _products.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(SerialTrackedProduct());
        _serials.GetExistingSerialNumbersAsync(ProductId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());
        var sut = new RegisterSerialUnitsCommandHandler(_products, _serials);

        var count = await sut.Handle(
            new RegisterSerialUnitsCommand(ProductId, new[] { "SN-1", "SN-2", "SN-2" }, UnitCost: 12m), default);

        count.Should().Be(2); // deduped
        await _serials.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<SerialUnit>>(u => u.Count() == 2 && u.All(x => x.Status == SerialStatus.InStock && x.UnitCost == 12m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_rejects_a_non_serial_tracked_product()
    {
        var product = new Product("SKU-A", "Widget", "pcs", 10m, "TRY") { Id = ProductId };
        _products.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);
        var sut = new RegisterSerialUnitsCommandHandler(_products, _serials);

        var act = () => sut.Handle(new RegisterSerialUnitsCommand(ProductId, new[] { "SN-1" }), default);

        await act.Should().ThrowAsync<ProductNotSerialTrackedException>();
    }

    [Fact]
    public async Task Register_rejects_duplicate_serials()
    {
        _products.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(SerialTrackedProduct());
        _serials.GetExistingSerialNumbersAsync(ProductId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { "SN-1" });
        var sut = new RegisterSerialUnitsCommandHandler(_products, _serials);

        var act = () => sut.Handle(new RegisterSerialUnitsCommand(ProductId, new[] { "SN-1" }), default);

        await act.Should().ThrowAsync<DuplicateSerialUnitException>();
    }

    [Fact]
    public async Task Ship_marks_units_shipped_and_stamps_where_used()
    {
        var unit = new SerialUnit(ProductId, "SN-1", DateTime.UtcNow) { Id = Guid.NewGuid() };
        _serials.GetBySerialNumbersAsync(ProductId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { unit });
        var sut = new ShipSerialUnitsCommandHandler(_serials);

        var count = await sut.Handle(
            new ShipSerialUnitsCommand(ProductId, new[] { "SN-1" }, OrderId, CustomerId: CustomerId), default);

        count.Should().Be(1);
        unit.Status.Should().Be(SerialStatus.Shipped);
        unit.OrderId.Should().Be(OrderId);
        unit.CurrentOwnerCustomerId.Should().Be(CustomerId);
    }

    [Fact]
    public async Task Ship_throws_when_a_serial_is_not_found()
    {
        _serials.GetBySerialNumbersAsync(ProductId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SerialUnit>());
        var sut = new ShipSerialUnitsCommandHandler(_serials);

        var act = () => sut.Handle(new ShipSerialUnitsCommand(ProductId, new[] { "SN-404" }, OrderId), default);

        await act.Should().ThrowAsync<SerialUnitNotFoundException>();
    }

    [Fact]
    public async Task Ship_rejects_an_already_shipped_unit()
    {
        var unit = new SerialUnit(ProductId, "SN-1", DateTime.UtcNow) { Id = Guid.NewGuid() };
        unit.Ship(Guid.NewGuid(), null, null, DateTime.UtcNow);
        _serials.GetBySerialNumbersAsync(ProductId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { unit });
        var sut = new ShipSerialUnitsCommandHandler(_serials);

        var act = () => sut.Handle(new ShipSerialUnitsCommand(ProductId, new[] { "SN-1" }, OrderId), default);

        await act.Should().ThrowAsync<SerialUnitTransitionException>();
    }

    [Fact]
    public async Task Where_used_returns_status_owner_and_component_genealogy()
    {
        var assembly = new SerialUnit(ProductId, "ASM-1", DateTime.UtcNow) { Id = Guid.NewGuid() };
        assembly.Ship(OrderId, null, CustomerId, DateTime.UtcNow);
        var component = new SerialUnit(Guid.NewGuid(), "CMP-1", DateTime.UtcNow, parentSerialUnitId: assembly.Id)
        {
            Id = Guid.NewGuid()
        };
        _serials.GetBySerialNumberAsync("ASM-1", Arg.Any<CancellationToken>()).Returns(new[] { assembly });
        _serials.GetChildrenAsync(assembly.Id, Arg.Any<CancellationToken>()).Returns(new[] { component });
        var sut = new GetSerialWhereUsedQueryHandler(_serials);

        var result = await sut.Handle(new GetSerialWhereUsedQuery("ASM-1"), default);

        result.Should().ContainSingle();
        var dto = result.Single();
        dto.Status.Should().Be("Shipped");
        dto.OrderId.Should().Be(OrderId);
        dto.CurrentOwnerCustomerId.Should().Be(CustomerId);
        dto.Components.Should().ContainSingle(c => c.SerialNumber == "CMP-1");
    }
}
