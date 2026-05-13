using CoreAlign.Application.Products.Commands;
using CoreAlign.Application.Products.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Products;

public class ProductComponentHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IProductComponentRepository _componentRepository = Substitute.For<IProductComponentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AddProductComponentCommandHandler _sut;

    public ProductComponentHandlerTests()
    {
        _sut = new AddProductComponentCommandHandler(_productRepository, _componentRepository, _unitOfWork);
    }

    [Fact]
    public async Task Throws_when_parent_equals_component()
    {
        var id = Guid.NewGuid();
        var command = new AddProductComponentCommand(id, id, 1m, null);
        Func<Task> act = () => _sut.Handle(command, default);
        await act.Should().ThrowAsync<CircularProductComponentException>();
    }

    [Fact]
    public async Task Throws_on_duplicate()
    {
        var parent = BuildProduct("PARENT");
        var component = BuildProduct("CHILD");
        _productRepository.GetByIdAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);
        _productRepository.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        _componentRepository.ExistsAsync(parent.Id, component.Id, Arg.Any<CancellationToken>()).Returns(true);

        var command = new AddProductComponentCommand(parent.Id, component.Id, 1m, null);
        Func<Task> act = () => _sut.Handle(command, default);
        await act.Should().ThrowAsync<DuplicateProductComponentException>();
    }

    [Fact]
    public async Task Throws_on_cycle()
    {
        var parent = BuildProduct("A");
        var component = BuildProduct("B");
        _productRepository.GetByIdAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);
        _productRepository.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        _componentRepository.ExistsAsync(parent.Id, component.Id, Arg.Any<CancellationToken>()).Returns(false);
        _componentRepository.WouldCreateCycleAsync(parent.Id, component.Id, Arg.Any<CancellationToken>()).Returns(true);

        var command = new AddProductComponentCommand(parent.Id, component.Id, 1m, null);
        Func<Task> act = () => _sut.Handle(command, default);
        await act.Should().ThrowAsync<CircularProductComponentException>();
    }

    [Fact]
    public async Task Adds_and_returns_dto_when_valid()
    {
        var parent = BuildProduct("PARENT");
        var component = BuildProduct("CHILD");
        _productRepository.GetByIdAsync(parent.Id, Arg.Any<CancellationToken>()).Returns(parent);
        _productRepository.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        _componentRepository.ExistsAsync(parent.Id, component.Id, Arg.Any<CancellationToken>()).Returns(false);
        _componentRepository.WouldCreateCycleAsync(parent.Id, component.Id, Arg.Any<CancellationToken>()).Returns(false);

        var command = new AddProductComponentCommand(parent.Id, component.Id, 2.5m, "screws");
        var result = await _sut.Handle(command, default);

        result.ParentProductId.Should().Be(parent.Id);
        result.ComponentProductId.Should().Be(component.Id);
        result.Quantity.Should().Be(2.5m);
        result.ComponentSku.Should().Be("CHILD");
        await _componentRepository.Received(1).AddAsync(Arg.Any<ProductComponent>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Product BuildProduct(string sku)
    {
        return new Product(sku, $"Product {sku}", "pcs", 1m, "USD", 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };
    }
}
