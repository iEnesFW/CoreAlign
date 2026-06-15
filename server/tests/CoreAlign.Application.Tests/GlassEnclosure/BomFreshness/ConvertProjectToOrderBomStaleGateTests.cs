using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.Stock.Availability;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.BomFreshness;

public class ConvertProjectToOrderBomStaleGateTests
{
    private readonly IGlassProjectRepository _projectRepo = Substitute.For<IGlassProjectRepository>();
    private readonly IGlassProjectOrderLinkRepository _linkRepo = Substitute.For<IGlassProjectOrderLinkRepository>();
    private readonly IGlassProjectBOMLineRepository _bomLineRepo = Substitute.For<IGlassProjectBOMLineRepository>();
    private readonly IOrderRepository _orderRepo = Substitute.For<IOrderRepository>();
    private readonly IProductRepository _productRepo = Substitute.For<IProductRepository>();
    private readonly IDocumentSequenceRepository _sequenceRepo = Substitute.For<IDocumentSequenceRepository>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IStockAvailabilityService _availabilityService = Substitute.For<IStockAvailabilityService>();
    private readonly ConvertProjectToOrderCommandHandler _sut;

    public ConvertProjectToOrderBomStaleGateTests()
    {
        _linkRepo.GetByProjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GlassProjectOrderLink?)null);
        _sequenceRepo.ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("SO-000001");
        _currentUser.UserId.Returns(Guid.NewGuid());
        _availabilityService.CheckAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockAvailabilityRow>());

        _sut = new ConvertProjectToOrderCommandHandler(
            _projectRepo, _linkRepo, _bomLineRepo, _orderRepo, _productRepo, _sequenceRepo, _currentUser, _availabilityService);
    }

    [Fact]
    public async Task Throws_when_bom_is_stale_and_force_flag_is_false()
    {
        var project = BuildQuotedProject();
        project.MarkBomStale(BomStaleReason.RunChanged.ToString(), DateTime.UtcNow);
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        SeedSingleProductBomLine(project);

        var act = async () => await _sut.Handle(
            new ConvertProjectToOrderCommand(project.Id, ForceConvertWithShortage: false, ForceWithStaleBom: false),
            default);

        var ex = await act.Should().ThrowAsync<BomStaleBlocksConvertException>();
        ex.Which.StaleReason.Should().Be(BomStaleReason.RunChanged.ToString());
    }

    [Fact]
    public async Task Succeeds_when_bom_is_stale_and_force_flag_is_true()
    {
        var project = BuildQuotedProject();
        project.MarkBomStale(BomStaleReason.PanelChanged.ToString(), DateTime.UtcNow);
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        SeedSingleProductBomLine(project);

        var result = await _sut.Handle(
            new ConvertProjectToOrderCommand(project.Id, ForceConvertWithShortage: false, ForceWithStaleBom: true),
            default);

        result.Should().NotBeNull();
        result.ProjectId.Should().Be(project.Id);
        result.OrderNumber.Should().Be("SO-000001");
    }

    [Fact]
    public async Task Does_not_throw_when_bom_is_fresh()
    {
        var project = BuildQuotedProject();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        SeedSingleProductBomLine(project);

        var result = await _sut.Handle(
            new ConvertProjectToOrderCommand(project.Id, ForceConvertWithShortage: false, ForceWithStaleBom: false),
            default);

        result.Should().NotBeNull();
        result.ProjectId.Should().Be(project.Id);
    }

    private void SeedSingleProductBomLine(GlassProject project)
    {
        var product = new Product("PRD-1", "Item", "pcs", 0m, "TRY", initialStock: 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };
        var line = new GlassProjectBOMLine(
            project.Id, GlassBOMLineKind.HardwarePiece, "Item", 1m, "Piece", 10m, "TRY",
            productId: product.Id, sortOrder: 0);
        _bomLineRepo.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { line });
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [product.Id] = product });
    }

    private static GlassProject BuildQuotedProject()
    {
        var project = new GlassProject(
            code: "PRJ-1",
            customerId: Guid.NewGuid(),
            projectName: "Convert Stale Gate",
            createdByUserId: Guid.NewGuid());
        project.TransitionTo(GlassProjectStatus.Quoted, Guid.NewGuid());
        return project;
    }
}
