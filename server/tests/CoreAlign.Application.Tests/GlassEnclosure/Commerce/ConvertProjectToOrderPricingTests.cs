using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.Stock.Availability;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.Commerce;

public class ConvertProjectToOrderPricingTests
{
    private readonly IGlassProjectRepository _projectRepo = Substitute.For<IGlassProjectRepository>();
    private readonly IGlassProjectOrderLinkRepository _linkRepo = Substitute.For<IGlassProjectOrderLinkRepository>();
    private readonly IGlassProjectBOMLineRepository _bomLineRepo = Substitute.For<IGlassProjectBOMLineRepository>();
    private readonly IOrderRepository _orderRepo = Substitute.For<IOrderRepository>();
    private readonly IProductRepository _productRepo = Substitute.For<IProductRepository>();
    private readonly IDocumentSequenceRepository _sequenceRepo = Substitute.For<IDocumentSequenceRepository>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IStockAvailabilityService _availabilityService = Substitute.For<IStockAvailabilityService>();
    private readonly IGlassEnclosureSettingsRepository _settingsRepo = Substitute.For<IGlassEnclosureSettingsRepository>();
    private readonly ConvertProjectToOrderCommandHandler _sut;

    private const decimal MarginPercent = 20m;
    private static readonly Guid TenantId = Guid.NewGuid();

    public ConvertProjectToOrderPricingTests()
    {
        _linkRepo.GetByProjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GlassProjectOrderLink?)null);
        _sequenceRepo.ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("SO-000099");
        _currentUser.UserId.Returns(Guid.NewGuid());
        _availabilityService.CheckAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockAvailabilityRow>());

        var settings = new GlassEnclosureSettings(TenantId);
        settings.UpdateCore(
            defaultStockBarLengthMm: 6000,
            defaultJumboGlassWidthMm: 3210,
            defaultJumboGlassHeightMm: 2250,
            sawKerfMm: 5m,
            glassKerfMm: 4m,
            guillotineRequired: true,
            defaultWastePercent: 5m,
            laborCostPerM2: 0m,
            defaultMarginPercent: MarginPercent);
        _settingsRepo.GetOrCreateForCurrentTenantAsync(Arg.Any<CancellationToken>()).Returns(settings);

        _sut = new ConvertProjectToOrderCommandHandler(
            _projectRepo, _linkRepo, _bomLineRepo, _orderRepo, _productRepo,
            _sequenceRepo, _currentUser, _availabilityService, _settingsRepo);
    }

    [Fact]
    public async Task Applies_margin_and_tax_so_order_subtotal_matches_cost_times_margin_multiplier()
    {
        var project = BuildQuotedProject();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var product = new Product("PRD-1", "Hardware", "pcs", 0m, "TRY", initialStock: 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId
        };
        var bomLine = new GlassProjectBOMLine(
            project.Id,
            GlassBOMLineKind.HardwarePiece,
            "Item",
            quantity: 2m,
            unit: "Piece",
            unitCost: 50m,
            currency: "TRY",
            productId: product.Id,
            sortOrder: 0);

        _bomLineRepo.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { bomLine });
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [product.Id] = product });

        Order? captured = null;
        await _orderRepo.AddAsync(
            Arg.Do<Order>(o => captured = o),
            Arg.Any<CancellationToken>());

        await _sut.Handle(
            new ConvertProjectToOrderCommand(project.Id, ForceConvertWithShortage: false, ForceWithStaleBom: false),
            default);

        captured.Should().NotBeNull();
        captured!.Lines.Should().HaveCount(1);
        var line = captured.Lines.Single();

        var expectedUnitPrice = 50m * (1 + (MarginPercent / 100m));
        line.UnitPrice.Should().Be(expectedUnitPrice);
        line.UnitCostSnapshot.Should().Be(50m);
        line.TaxRatePercent.Should().Be(20m);
        line.LineSubtotal.Should().Be(decimal.Round(2m * expectedUnitPrice, 4));
        line.TaxAmount.Should().Be(decimal.Round(2m * expectedUnitPrice * 0.20m, 4));
        captured.Total.Should().Be(decimal.Round(2m * expectedUnitPrice * 1.20m, 4));
    }

    [Fact]
    public async Task Substitute_selection_writes_chosen_product_and_records_audit_field()
    {
        var project = BuildQuotedProject();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var originalProduct = new Product("PRD-O", "Original", "pcs", 0m, "TRY", initialStock: 0m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId
        };
        var substituteProduct = new Product("PRD-S", "Substitute", "pcs", 0m, "TRY", initialStock: 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId
        };
        var bomLine = new GlassProjectBOMLine(
            project.Id,
            GlassBOMLineKind.HardwarePiece,
            "Item",
            quantity: 1m,
            unit: "Piece",
            unitCost: 30m,
            currency: "TRY",
            productId: originalProduct.Id,
            sortOrder: 0);

        _bomLineRepo.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { bomLine });
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [originalProduct.Id] = originalProduct,
                [substituteProduct.Id] = substituteProduct,
            });

        Order? captured = null;
        await _orderRepo.AddAsync(
            Arg.Do<Order>(o => captured = o),
            Arg.Any<CancellationToken>());

        var selections = new Dictionary<Guid, Guid> { [bomLine.Id] = substituteProduct.Id };
        await _sut.Handle(
            new ConvertProjectToOrderCommand(
                project.Id,
                ForceConvertWithShortage: false,
                ForceWithStaleBom: false,
                SubstituteSelections: selections),
            default);

        captured.Should().NotBeNull();
        var line = captured!.Lines.Single();
        line.ProductId.Should().Be(substituteProduct.Id);
        line.SubstituteFromProductId.Should().Be(originalProduct.Id);
        line.ProductSku.Should().Be("PRD-S");
    }

    [Fact]
    public async Task Service_line_carries_zero_tax_and_no_substitute_audit()
    {
        var project = BuildQuotedProject();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var bomLine = new GlassProjectBOMLine(
            project.Id,
            GlassBOMLineKind.Labor,
            "Workshop labor",
            quantity: 1m,
            unit: "lot",
            unitCost: 100m,
            currency: "TRY",
            productId: null,
            isService: true,
            sortOrder: 0);

        _bomLineRepo.ListByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { bomLine });
        _productRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>());

        Order? captured = null;
        await _orderRepo.AddAsync(
            Arg.Do<Order>(o => captured = o),
            Arg.Any<CancellationToken>());

        await _sut.Handle(
            new ConvertProjectToOrderCommand(project.Id),
            default);

        var line = captured!.Lines.Single();
        line.IsService.Should().BeTrue();
        line.TaxRatePercent.Should().Be(0m);
        line.SubstituteFromProductId.Should().BeNull();
        line.UnitPrice.Should().Be(decimal.Round(100m * (1 + (MarginPercent / 100m)), 4));
    }

    private static GlassProject BuildQuotedProject()
    {
        var project = new GlassProject(
            code: "PRJ-PRICING",
            customerId: Guid.NewGuid(),
            projectName: "Pricing Fix",
            createdByUserId: Guid.NewGuid())
        {
            TenantId = TenantId
        };
        project.TransitionTo(GlassProjectStatus.Quoted, Guid.NewGuid());
        return project;
    }
}
