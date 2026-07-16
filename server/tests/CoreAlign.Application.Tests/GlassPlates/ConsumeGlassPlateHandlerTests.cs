using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.GlassPlates.Commands;
using CoreAlign.Application.GlassPlates.Handlers;
using CoreAlign.Application.GlassPlates.Notifications;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassPlates;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CoreAlign.Application.Tests.GlassPlates;

public class ConsumeGlassPlateHandlerTests
{
    private readonly IGlassPlateRepository _plates = Substitute.For<IGlassPlateRepository>();
    private readonly IGlassPlateConsumptionRepository _consumptions =
        Substitute.For<IGlassPlateConsumptionRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IAllocationService _allocation = Substitute.For<IAllocationService>();
    private readonly IStockReasonCodeRepository _reasons = Substitute.For<IStockReasonCodeRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly IGlassPlateDepletionNotifier _notifier =
        Substitute.For<IGlassPlateDepletionNotifier>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private static StockMovement StubMovement(StockMovementType type, decimal qty) =>
        new(Guid.NewGuid(), WarehouseId, type, qty, 5m, 100m, 5m, DateTime.UtcNow,
            StockSourceDocumentType.Production);

    private ConsumeGlassPlateHandler CreateSut() => new(
        _plates, _consumptions, _products, _allocation, _reasons, _outbox, _configuration,
        _notifier, _tenant, _uow);

    private (Product product, GlassPlate plate) Arrange(decimal? minRemnantAreaMm2)
    {
        var product = new Product("PLATE-1", "Glass 4mm", price: 100m);
        product.SetPlateTracking(true, minRemnantAreaMm2, null, null, null, null, null);
        var plate = new GlassPlate(product.Id, WarehouseId, "PL-1", 2000m, 1000m, 4m,
            PlateKind.Fresh, DateTime.UtcNow);

        _tenant.RequireTenantId().Returns(TenantId);
        _plates.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(plate);
        _products.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(product);
        _plates.CountAvailableAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>()).Returns(3);
        return (product, plate);
    }

    private static ConsumeGlassPlateCommand Command(
        Guid plateId,
        decimal cutAreaMm2,
        decimal? remnantWidthMm = null,
        decimal? remnantHeightMm = null) =>
        new(plateId, cutAreaMm2, 1, null, null, remnantWidthMm, remnantHeightMm, "RM-1",
            null, null, null, null, Guid.NewGuid());

    [Fact]
    public async Task Remnant_meeting_minimum_reduces_issue_and_creates_no_scrap()
    {
        var (_, plate) = Arrange(minRemnantAreaMm2: 100_000m);
        StockIssueRequest? issued = null;
        _allocation.ApplyIssueAsync(Arg.Do<StockIssueRequest>(r => issued = r), Arg.Any<CancellationToken>())
            .Returns(StubMovement(StockMovementType.Issue, 1.52m));

        var result = await CreateSut().Handle(
            Command(plate.Id, cutAreaMm2: 500_000m, remnantWidthMm: 800m, remnantHeightMm: 600m),
            default);

        issued!.Quantity.Should().Be(1.52m);
        result.RemnantPlateId.Should().NotBeNull();
        result.RemnantAreaMm2.Should().Be(480_000m);
        result.ScrappedAreaMm2.Should().Be(0m);
        plate.Status.Should().Be(GlassPlateStatus.Consumed);
        await _plates.Received(1).AddAsync(Arg.Any<GlassPlate>(), Arg.Any<CancellationToken>());
        await _allocation.DidNotReceive()
            .AdjustAsync(Arg.Any<StockAdjustmentRequest>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().EnqueueAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Below_minimum_remnant_becomes_scrap_write_off()
    {
        var (_, plate) = Arrange(minRemnantAreaMm2: 100_000m);
        _reasons.GetByCodeAsync("SCR-BELOWMIN", Arg.Any<CancellationToken>())
            .Returns(new StockReasonCode("SCR-BELOWMIN", "Below minimum offcut",
                StockReasonCategory.Scrap, affectsCost: true));
        StockIssueRequest? issued = null;
        StockAdjustmentRequest? adjusted = null;
        _allocation.ApplyIssueAsync(Arg.Do<StockIssueRequest>(r => issued = r), Arg.Any<CancellationToken>())
            .Returns(StubMovement(StockMovementType.Issue, 0.5m));
        _allocation.AdjustAsync(Arg.Do<StockAdjustmentRequest>(r => adjusted = r), Arg.Any<CancellationToken>())
            .Returns(StubMovement(StockMovementType.AdjustmentNegative, 1.5m));

        var result = await CreateSut().Handle(
            Command(plate.Id, cutAreaMm2: 500_000m, remnantWidthMm: 100m, remnantHeightMm: 100m),
            default);

        issued!.Quantity.Should().Be(0.5m);
        adjusted!.Delta.Should().Be(-1.5m);
        result.RemnantPlateId.Should().BeNull();
        result.ScrappedAreaMm2.Should().Be(1_500_000m);
        plate.Status.Should().Be(GlassPlateStatus.Consumed);
        await _outbox.Received(1).EnqueueAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Full_consume_issues_all_remaining_without_scrap()
    {
        var (_, plate) = Arrange(minRemnantAreaMm2: 100_000m);
        StockIssueRequest? issued = null;
        _allocation.ApplyIssueAsync(Arg.Do<StockIssueRequest>(r => issued = r), Arg.Any<CancellationToken>())
            .Returns(StubMovement(StockMovementType.Issue, 2m));

        var result = await CreateSut().Handle(Command(plate.Id, cutAreaMm2: 2_000_000m), default);

        issued!.Quantity.Should().Be(2m);
        result.ScrappedAreaMm2.Should().Be(0m);
        result.RemnantPlateId.Should().BeNull();
        await _allocation.DidNotReceive()
            .AdjustAsync(Arg.Any<StockAdjustmentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cut_exceeding_remaining_beyond_tolerance_throws()
    {
        var (_, plate) = Arrange(minRemnantAreaMm2: 100_000m);

        var act = () => CreateSut().Handle(Command(plate.Id, cutAreaMm2: 2_000_100m), default);

        await act.Should().ThrowAsync<GlassPlateAreaExceededException>();
        await _allocation.DidNotReceive()
            .ApplyIssueAsync(Arg.Any<StockIssueRequest>(), Arg.Any<CancellationToken>());
    }
}
