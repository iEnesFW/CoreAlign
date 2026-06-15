using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.CustomerPortal;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class DealerProductVisibilityHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid DealerId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly IDealerCustomerLinkRepository _links = Substitute.For<IDealerCustomerLinkRepository>();
    private readonly ICustomerDealerProductVisibilityRepository _visibility = Substitute.For<ICustomerDealerProductVisibilityRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private DealerCustomerLink BuildLink(Guid? id = null, Guid? customerOverride = null)
    {
        var link = new DealerCustomerLink(DealerId, customerOverride ?? CustomerId, assignedByUserId: null);
        typeof(DealerCustomerLink).GetProperty(nameof(DealerCustomerLink.Id))!.SetValue(link, id ?? Guid.NewGuid());
        typeof(DealerCustomerLink).GetProperty(nameof(DealerCustomerLink.TenantId))!.SetValue(link, TenantId);
        return link;
    }

    [Fact]
    public async Task Get_returns_All_mode_when_no_whitelist_exists()
    {
        var link = BuildLink();
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _links.GetByIdAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);
        _visibility.HasAnyForLinkAsync(link.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new GetDealerProductVisibilityHandler(_scope, _links, _visibility);

        var result = await handler.Handle(new GetDealerProductVisibilityQuery(link.Id), default);

        result.Mode.Should().Be(DealerProductVisibilityModes.All);
        result.VisibleProductIds.Should().BeEmpty();
        result.LinkId.Should().Be(link.Id);
    }

    [Fact]
    public async Task Get_returns_Whitelist_mode_and_ids_when_present()
    {
        var link = BuildLink();
        var productIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _links.GetByIdAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);
        _visibility.HasAnyForLinkAsync(link.Id, Arg.Any<CancellationToken>()).Returns(true);
        _visibility.ListVisibleProductIdsAsync(link.Id, Arg.Any<CancellationToken>()).Returns(productIds);

        var handler = new GetDealerProductVisibilityHandler(_scope, _links, _visibility);

        var result = await handler.Handle(new GetDealerProductVisibilityQuery(link.Id), default);

        result.Mode.Should().Be(DealerProductVisibilityModes.Whitelist);
        result.VisibleProductIds.Should().BeEquivalentTo(productIds);
    }

    [Fact]
    public async Task Set_whitelist_with_three_products_creates_three_rows()
    {
        var link = BuildLink();
        var productIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var products = productIds.ToDictionary(id => id, id => BuildProduct(id));

        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _links.GetByIdAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);
        _visibility.ListByLinkAsync(link.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CustomerDealerProductVisibility>());
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(products);

        var handler = new SetDealerProductVisibilityHandler(_scope, _links, _visibility, _products, _uow);

        var result = await handler.Handle(
            new SetDealerProductVisibilityCommand(link.Id, DealerProductVisibilityModes.Whitelist, productIds),
            default);

        result.Mode.Should().Be(DealerProductVisibilityModes.Whitelist);
        result.VisibleProductIds.Should().BeEquivalentTo(productIds);
        await _visibility.Received(3).AddAsync(Arg.Any<CustomerDealerProductVisibility>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_whitelist_diff_adds_new_and_removes_dropped()
    {
        var link = BuildLink();
        var keepId = Guid.NewGuid();
        var removeId = Guid.NewGuid();
        var addId = Guid.NewGuid();

        var existing = new[]
        {
            new CustomerDealerProductVisibility(link.Id, keepId),
            new CustomerDealerProductVisibility(link.Id, removeId),
        };
        var products = new[] { keepId, addId }.ToDictionary(id => id, id => BuildProduct(id));

        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _links.GetByIdAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);
        _visibility.ListByLinkAsync(link.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(products);

        var handler = new SetDealerProductVisibilityHandler(_scope, _links, _visibility, _products, _uow);

        var result = await handler.Handle(
            new SetDealerProductVisibilityCommand(link.Id, DealerProductVisibilityModes.Whitelist, new[] { keepId, addId }),
            default);

        result.VisibleProductIds.Should().BeEquivalentTo(new[] { keepId, addId });
        await _visibility.Received(1).AddAsync(
            Arg.Is<CustomerDealerProductVisibility>(v => v.ProductId == addId),
            Arg.Any<CancellationToken>());
        await _visibility.Received(1).RemoveRangeAsync(
            Arg.Is<IEnumerable<CustomerDealerProductVisibility>>(items => items.Any(v => v.ProductId == removeId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_all_deletes_existing_rows()
    {
        var link = BuildLink();
        var existing = new[]
        {
            new CustomerDealerProductVisibility(link.Id, Guid.NewGuid()),
            new CustomerDealerProductVisibility(link.Id, Guid.NewGuid()),
        };

        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _links.GetByIdAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);
        _visibility.ListByLinkAsync(link.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new SetDealerProductVisibilityHandler(_scope, _links, _visibility, _products, _uow);

        var result = await handler.Handle(
            new SetDealerProductVisibilityCommand(link.Id, DealerProductVisibilityModes.All, Array.Empty<Guid>()),
            default);

        result.Mode.Should().Be(DealerProductVisibilityModes.All);
        result.VisibleProductIds.Should().BeEmpty();
        await _visibility.Received(1).RemoveRangeAsync(existing, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_rejects_when_caller_is_not_owner_of_links_customer()
    {
        var otherCustomerId = Guid.NewGuid();
        var link = BuildLink(customerOverride: otherCustomerId);

        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _links.GetByIdAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);

        var handler = new SetDealerProductVisibilityHandler(_scope, _links, _visibility, _products, _uow);

        var act = async () => await handler.Handle(
            new SetDealerProductVisibilityCommand(link.Id, DealerProductVisibilityModes.All, Array.Empty<Guid>()),
            default);

        await act.Should().ThrowAsync<B2BForbiddenException>();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Product BuildProduct(Guid id)
    {
        var product = new Product(sku: $"SKU-{id:N}".Substring(0, 12), name: "Test", unit: "adet", price: 1m, currency: "TRY");
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, id);
        typeof(Product).GetProperty(nameof(Product.TenantId))!.SetValue(product, TenantId);
        return product;
    }
}
