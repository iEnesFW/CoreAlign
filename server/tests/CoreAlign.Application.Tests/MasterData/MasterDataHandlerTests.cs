using CoreAlign.Application.MasterData.Commands;
using CoreAlign.Application.MasterData.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.MasterData;

public class MasterDataHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task CreateBrand_inserts_entity_and_saves()
    {
        var repo = Substitute.For<IBrandRepository>();
        var sut = new CreateBrandHandler(repo, _uow);

        var dto = await sut.Handle(new CreateBrandCommand("APL", "Apple", "Tech"), default);

        dto.Code.Should().Be("APL");
        dto.Name.Should().Be("Apple");
        await repo.Received(1).AddAsync(
            Arg.Is<Brand>(b => b.Code == "APL" && b.Name == "Apple"),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBrand_throws_when_not_found()
    {
        var repo = Substitute.For<IBrandRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Brand?)null);
        var sut = new UpdateBrandHandler(repo, _uow);

        Func<Task> act = () => sut.Handle(new UpdateBrandCommand(Guid.NewGuid(), "X", "Y", null, true), default);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateBrand_mutates_entity_when_found()
    {
        var existing = new Brand("OLD", "Old", null) { Id = Guid.NewGuid() };
        var repo = Substitute.For<IBrandRepository>();
        repo.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        var sut = new UpdateBrandHandler(repo, _uow);

        var dto = await sut.Handle(new UpdateBrandCommand(existing.Id, "NEW", "New", "desc", false), default);

        dto.Code.Should().Be("NEW");
        dto.IsActive.Should().BeFalse();
        repo.Received(1).Update(existing);
    }

    [Fact]
    public async Task DeleteBrand_returns_false_when_not_found()
    {
        var repo = Substitute.For<IBrandRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Brand?)null);
        var sut = new DeleteBrandHandler(repo, _uow);

        var result = await sut.Handle(new DeleteBrandCommand(Guid.NewGuid()), default);

        result.Should().BeFalse();
        repo.DidNotReceive().Remove(Arg.Any<Brand>());
    }

    [Fact]
    public async Task DeleteBrand_removes_and_saves_when_found()
    {
        var existing = new Brand("X", "x", null) { Id = Guid.NewGuid() };
        var repo = Substitute.For<IBrandRepository>();
        repo.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        var sut = new DeleteBrandHandler(repo, _uow);

        var ok = await sut.Handle(new DeleteBrandCommand(existing.Id), default);

        ok.Should().BeTrue();
        repo.Received(1).Remove(existing);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateWarehouse_inserts_with_type_and_default_flag()
    {
        var repo = Substitute.For<IWarehouseRepository>();
        var sut = new CreateWarehouseHandler(repo, _uow);

        await sut.Handle(new CreateWarehouseCommand("WH1", "Main", WarehouseType.Main, IsDefault: true), default);

        await repo.Received(1).AddAsync(
            Arg.Is<Warehouse>(w => w.Code == "WH1" && w.IsDefault),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePaymentTerm_rejects_negative_net_days_via_domain()
    {
        var repo = Substitute.For<IPaymentTermRepository>();
        var sut = new CreatePaymentTermHandler(repo, _uow);

        Func<Task> act = () => sut.Handle(new CreatePaymentTermCommand("N-15", "Net -15", -15), default);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateTaxRate_rejects_rate_outside_0_to_100_via_domain()
    {
        var repo = Substitute.For<ITaxRateRepository>();
        var sut = new CreateTaxRateHandler(repo, _uow);

        Func<Task> overRange = () => sut.Handle(new CreateTaxRateCommand("KDV200", "KDV 200", 200m), default);
        await overRange.Should().ThrowAsync<ArgumentException>();

        Func<Task> negative = () => sut.Handle(new CreateTaxRateCommand("KDV-1", "Negative", -1m), default);
        await negative.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreatePriceList_inserts_with_currency_and_validity()
    {
        var repo = Substitute.For<IPriceListRepository>();
        var sut = new CreatePriceListHandler(repo, _uow);

        var validFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await sut.Handle(new CreatePriceListCommand("PL-USD", "USD List", "USD", IsTaxInclusive: true,
            ValidFromUtc: validFrom, ValidUntilUtc: null, IsDefault: true), default);

        await repo.Received(1).AddAsync(
            Arg.Is<PriceList>(p => p.Code == "PL-USD" && p.Currency == "USD" && p.IsTaxInclusive && p.IsDefault),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUnitOfMeasure_rejects_non_positive_conversion_factor()
    {
        var repo = Substitute.For<IUnitOfMeasureRepository>();
        var sut = new CreateUnitOfMeasureHandler(repo, _uow);

        Func<Task> act = () => sut.Handle(new CreateUnitOfMeasureCommand(
            "KG", "Kilogram", "kg", null, ConversionFactor: 0m, DecimalPlaces: 3), default);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateProductCategory_inserts_with_optional_parent()
    {
        var repo = Substitute.For<IProductCategoryRepository>();
        var sut = new CreateProductCategoryHandler(repo, _uow);

        var parent = Guid.NewGuid();
        await sut.Handle(new CreateProductCategoryCommand("CHILD", "Child", parent, "desc"), default);

        await repo.Received(1).AddAsync(
            Arg.Is<ProductCategory>(c => c.Code == "CHILD" && c.ParentCategoryId == parent),
            Arg.Any<CancellationToken>());
    }
}
