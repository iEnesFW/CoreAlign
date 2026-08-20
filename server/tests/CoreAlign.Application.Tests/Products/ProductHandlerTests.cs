using CoreAlign.Application.Inventory.Services;
using CoreAlign.Application.Products.Commands;
using CoreAlign.Application.Products.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Products;

public class ProductHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IInventoryCostingService _costing = Substitute.For<IInventoryCostingService>();
    private readonly CreateProductCommandHandler _createSut;
    private readonly UpdateProductCommandHandler _updateSut;
    private readonly DeleteProductCommandHandler _deleteSut;

    public ProductHandlerTests()
    {
        _createSut = new CreateProductCommandHandler(_productRepository, _unitOfWork);
        _updateSut = new UpdateProductCommandHandler(_productRepository, _costing, _unitOfWork);
        _deleteSut = new DeleteProductCommandHandler(_productRepository, _unitOfWork);
    }

    [Fact]
    public async Task Create_throws_on_duplicate_sku()
    {
        _productRepository.SkuExistsAsync("SKU-X", null, Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateProductCommand(Sku: "SKU-X", Name: "Widget", Unit: "pcs", Price: 10m, Currency: "TRY", StockQuantity: 5m);
        Func<Task> act = () => _createSut.Handle(command, default);
        await act.Should().ThrowAsync<DuplicateProductSkuException>();
    }

    [Fact]
    public async Task Create_persists_and_returns_dto()
    {
        _productRepository.SkuExistsAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateProductCommand(
            Sku: "SKU-A",
            Name: "Widget",
            Description: "desc",
            Unit: "pcs",
            Price: 12m,
            Currency: "TRY",
            StockQuantity: 50m);
        var result = await _createSut.Handle(command, default);

        result.Sku.Should().Be("SKU-A");
        result.Name.Should().Be("Widget");
        result.StockQuantity.Should().Be(50m);
        result.ProcurementType.Should().Be(ProcurementType.Buy);
        await _productRepository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_persists_make_procurement_type()
    {
        _productRepository.SkuExistsAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);
        Product? captured = null;
        await _productRepository.AddAsync(Arg.Do<Product>(p => captured = p), Arg.Any<CancellationToken>());

        var command = new CreateProductCommand(
            Sku: "SKU-MAKE",
            Name: "Assembly",
            Unit: "pcs",
            Price: 100m,
            Currency: "TRY",
            ProcurementType: ProcurementType.Make);
        var result = await _createSut.Handle(command, default);

        result.ProcurementType.Should().Be(ProcurementType.Make);
        captured!.ProcurementType.Should().Be(ProcurementType.Make);
    }

    [Fact]
    public async Task Create_persists_glass_color_and_thickness_trimmed()
    {
        _productRepository.SkuExistsAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);
        Product? captured = null;
        await _productRepository.AddAsync(Arg.Do<Product>(p => captured = p), Arg.Any<CancellationToken>());

        var command = new CreateProductCommand(
            Sku: "SKU-4MM",
            Name: "4mm cam",
            Unit: "m2",
            Price: 100m,
            Currency: "TRY",
            Color: "  Bronz  ",
            ThicknessMm: 4m);
        var result = await _createSut.Handle(command, default);

        captured!.Color.Should().Be("Bronz");
        captured.ThicknessMm.Should().Be(4m);
        result.Color.Should().Be("Bronz");
        result.ThicknessMm.Should().Be(4m);
    }

    // Switching an already-stocked product to FIFO leaves it with no cost layers, so the next
    // issue hits the exhausted-layer hard error and the product stops being sellable.
    [Fact]
    public async Task Switching_to_fifo_seeds_opening_cost_layers()
    {
        var product = new Product("SKU-A", "Widget", "pcs", 10m, "TRY", 0m) { Id = Guid.NewGuid() };
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _updateSut.Handle(BuildUpdate(product.Id, "SKU-A") with { CostingMethod = CostingMethod.Fifo }, default);

        await _costing.Received(1).SeedOpeningLayersAsync(product, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_update_that_leaves_the_costing_method_alone_seeds_nothing()
    {
        var product = new Product("SKU-A", "Widget", "pcs", 10m, "TRY", 0m) { Id = Guid.NewGuid() };
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        await _updateSut.Handle(BuildUpdate(product.Id, "SKU-A"), default);

        await _costing.DidNotReceive().SeedOpeningLayersAsync(
            Arg.Any<Product>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    private static UpdateProductCommand BuildUpdate(Guid id, string sku) =>
        new(
            Id: id,
            Sku: sku,
            Name: "Widget",
            Description: null,
            ShortDescription: null,
            Barcode: null,
            Mpn: null,
            Slug: null,
            BrandId: null,
            CategoryId: null,
            ParentProductId: null,
            VariantAttributesJson: null,
            TagsJson: null,
            Unit: "pcs",
            BaseUomId: null,
            PurchaseUomId: null,
            SalesUomId: null,
            Price: 10m,
            ListPrice: 10m,
            MinSellingPrice: 0m,
            StandardCost: 0m,
            Currency: "TRY",
            TaxRateId: null,
            IsPriceTaxInclusive: false,
            IsStockTracked: true,
            IsLotTracked: false,
            IsSerialTracked: false,
            RequiresInspection: null,
            MinStock: 0m,
            MaxStock: 0m,
            ReorderPoint: 0m,
            SafetyStock: 0m,
            LeadTimeDays: 0,
            WeightKg: null,
            WidthCm: null,
            HeightCm: null,
            DepthCm: null,
            VolumeM3: null,
            Status: ProductStatus.Active,
            LaunchDate: null,
            EndOfLifeDate: null);

    [Fact]
    public async Task Update_throws_when_product_not_found()
    {
        _productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);
        Func<Task> act = () => _updateSut.Handle(BuildUpdate(Guid.NewGuid(), "SKU-A"), default);
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Update_throws_when_changing_to_existing_sku()
    {
        var existing = new Product("SKU-OLD", "Widget", "pcs", 10m, "TRY", 0m) { Id = Guid.NewGuid() };
        _productRepository.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _productRepository.SkuExistsAsync("SKU-TAKEN", existing.Id, Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = () => _updateSut.Handle(BuildUpdate(existing.Id, "SKU-TAKEN"), default);
        await act.Should().ThrowAsync<DuplicateProductSkuException>();
    }

    [Fact]
    public async Task Delete_throws_when_not_found()
    {
        _productRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);
        Func<Task> act = () => _deleteSut.Handle(new DeleteProductCommand(Guid.NewGuid()), default);
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }
}
