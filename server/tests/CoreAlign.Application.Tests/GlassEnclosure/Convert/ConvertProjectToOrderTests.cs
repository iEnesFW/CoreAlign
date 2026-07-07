using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.Stock.Availability;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.Convert;

public class ConvertProjectToOrderTests
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

    public ConvertProjectToOrderTests()
    {
        _linkRepo.GetByProjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GlassProjectOrderLink?)null);
        _sequenceRepo.ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("SO-000001");
        _currentUser.UserId.Returns(Guid.NewGuid());

        _availabilityService.CheckAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockAvailabilityRow>());

        _sut = new ConvertProjectToOrderCommandHandler(
            _projectRepo, _linkRepo, _bomLineRepo, _orderRepo, _productRepo, _sequenceRepo, _currentUser, _availabilityService);
    }

    [Fact]
    public async Task Throws_EmptyBomException_when_bom_has_no_lines()
    {
        var project = BuildQuotedProject();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _bomLineRepo.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GlassProjectBOMLine>());

        Func<Task> act = () => _sut.Handle(new ConvertProjectToOrderCommand(project.Id), default);

        await act.Should().ThrowAsync<EmptyBomException>();
    }

    [Fact]
    public async Task Throws_BomLineProductLinkMissingException_when_nonservice_line_has_no_product_id()
    {
        var project = BuildQuotedProject();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var unlinked = new GlassProjectBOMLine(
            project.Id, GlassBOMLineKind.GlassPiece, "Tempered 6mm", 1m, "Piece", 100m, "TRY",
            productId: null, isService: false, sortOrder: 0);
        _bomLineRepo.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { unlinked });

        Func<Task> act = () => _sut.Handle(new ConvertProjectToOrderCommand(project.Id), default);

        var ex = await act.Should().ThrowAsync<BomLineProductLinkMissingException>();
        ex.Which.BomLineId.Should().Be(unlinked.Id);
    }

    [Fact]
    public async Task Creates_one_order_line_per_bom_line_with_source_bom_line_id()
    {
        var project = BuildQuotedProject();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var glassProduct = BuildProduct("GLS-6", "Tempered 6");
        var hardwareProduct = BuildProduct("HDW-HG", "Hinge");
        var profileProduct = BuildProduct("PRF-A", "Profile A");

        var glassLine = new GlassProjectBOMLine(
            project.Id, GlassBOMLineKind.GlassPiece, "Tempered 6", 2m, "Piece", 100m, "TRY",
            productId: glassProduct.Id, sortOrder: 0);
        var hardwareLine = new GlassProjectBOMLine(
            project.Id, GlassBOMLineKind.HardwarePiece, "Hinge", 4m, "Piece", 25m, "TRY",
            productId: hardwareProduct.Id, sortOrder: 1);
        var profileLine = new GlassProjectBOMLine(
            project.Id, GlassBOMLineKind.ProfileCut, "Profile A 2m", 3m, "Piece", 50m, "TRY",
            productId: profileProduct.Id, sortOrder: 2);

        _bomLineRepo.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { glassLine, hardwareLine, profileLine });
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [glassProduct.Id] = glassProduct,
                [hardwareProduct.Id] = hardwareProduct,
                [profileProduct.Id] = profileProduct
            });

        Order? captured = null;
        await _orderRepo.AddAsync(Arg.Do<Order>(o => captured = o), Arg.Any<CancellationToken>());

        await _sut.Handle(new ConvertProjectToOrderCommand(project.Id), default);

        captured.Should().NotBeNull();
        captured!.Lines.Should().HaveCount(3);
        captured.Lines.Should().Contain(l => l.SourceBomLineId == glassLine.Id && l.ProductId == glassProduct.Id);
        captured.Lines.Should().Contain(l => l.SourceBomLineId == hardwareLine.Id && l.ProductId == hardwareProduct.Id);
        captured.Lines.Should().Contain(l => l.SourceBomLineId == profileLine.Id && l.ProductId == profileProduct.Id);
        captured.Lines.Should().OnlyContain(l => l.SourceProjectId == project.Id);
        captured.Lines.Should().OnlyContain(l => !l.IsService);
    }

    [Fact]
    public async Task Service_line_produces_order_line_with_isservice_true_and_empty_product_id()
    {
        var project = BuildQuotedProject();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var glassProduct = BuildProduct("GLS-6", "Tempered 6");
        var profileProduct = BuildProduct("PRF-A", "Profile A");

        var glassLine = new GlassProjectBOMLine(
            project.Id, GlassBOMLineKind.GlassPiece, "Tempered 6", 2m, "Piece", 100m, "TRY",
            productId: glassProduct.Id, sortOrder: 0);
        var profileLine = new GlassProjectBOMLine(
            project.Id, GlassBOMLineKind.ProfileCut, "Profile A 2m", 3m, "Piece", 50m, "TRY",
            productId: profileProduct.Id, sortOrder: 1);
        var serviceLine = new GlassProjectBOMLine(
            project.Id, GlassBOMLineKind.Installation, "Installation", 1m, "Service", 500m, "TRY",
            productId: null, isService: true, sortOrder: 2);

        _bomLineRepo.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { glassLine, profileLine, serviceLine });
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [glassProduct.Id] = glassProduct,
                [profileProduct.Id] = profileProduct
            });

        Order? captured = null;
        await _orderRepo.AddAsync(Arg.Do<Order>(o => captured = o), Arg.Any<CancellationToken>());

        await _sut.Handle(new ConvertProjectToOrderCommand(project.Id), default);

        captured.Should().NotBeNull();
        captured!.Lines.Should().HaveCount(3);

        var serviceOrderLine = captured.Lines.Single(l => l.SourceBomLineId == serviceLine.Id);
        serviceOrderLine.IsService.Should().BeTrue();
        serviceOrderLine.ProductId.Should().Be(Guid.Empty);
        serviceOrderLine.ProductName.Should().Be("Installation");

        captured.Lines.Where(l => l.SourceBomLineId != serviceLine.Id).Should().OnlyContain(l => !l.IsService);
    }

    [Fact]
    public async Task Sets_source_glass_project_id_on_order_header()
    {
        var project = BuildQuotedProject();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var product = BuildProduct("PRD", "Item");
        var line = new GlassProjectBOMLine(
            project.Id, GlassBOMLineKind.HardwarePiece, "Item", 1m, "Piece", 10m, "TRY",
            productId: product.Id, sortOrder: 0);

        _bomLineRepo.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { line });
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [product.Id] = product });

        Order? captured = null;
        await _orderRepo.AddAsync(Arg.Do<Order>(o => captured = o), Arg.Any<CancellationToken>());

        await _sut.Handle(new ConvertProjectToOrderCommand(project.Id), default);

        captured!.SourceGlassProjectId.Should().Be(project.Id);
    }

    [Fact]
    public async Task Copies_currency_and_fx_rate_snapshot_from_project_to_order()
    {
        var project = BuildQuotedProject(currency: "EUR", fxRateToBase: 35.42m);
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var product = BuildProduct("PRD", "Item");
        var line = new GlassProjectBOMLine(
            project.Id, GlassBOMLineKind.HardwarePiece, "Item", 1m, "Piece", 10m, "EUR",
            productId: product.Id, sortOrder: 0);

        _bomLineRepo.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { line });
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [product.Id] = product });

        Order? captured = null;
        await _orderRepo.AddAsync(Arg.Do<Order>(o => captured = o), Arg.Any<CancellationToken>());

        await _sut.Handle(new ConvertProjectToOrderCommand(project.Id), default);

        captured!.Currency.Should().Be("EUR");
        captured.ExchangeRate.Should().Be(35.42m);
    }

    private static GlassProject BuildQuotedProject(string currency = "TRY", decimal fxRateToBase = 1m)
    {
        var project = new GlassProject(
            code: "PRJ-1",
            customerId: Guid.NewGuid(),
            projectName: "Test Project",
            createdByUserId: Guid.NewGuid(),
            currency: currency);
        if (fxRateToBase != 1m)
        {
            project.LockFxRate(fxRateToBase);
        }
        project.TransitionTo(GlassProjectStatus.Quoted, Guid.NewGuid());
        return project;
    }

    private static Product BuildProduct(string sku, string name)
    {
        return new Product(sku, name, "pcs", 0m, "TRY", initialStock: 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };
    }
}
