using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.DealerOrderFlow;
using CoreAlign.Application.B2B.DealerPortal;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class CreateDealerOrderHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid DealerUserId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly IDealerUserRepository _dealerUsers = Substitute.For<IDealerUserRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ICustomerAddressRepository _addresses = Substitute.For<ICustomerAddressRepository>();
    private readonly IPaymentTermRepository _paymentTerms = Substitute.For<IPaymentTermRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IDealerOrderApprovalOutbox _outbox = Substitute.For<IDealerOrderApprovalOutbox>();
    private readonly IDealerAccountRepository _dealers = Substitute.For<IDealerAccountRepository>();
    private readonly IPricingService _pricing = Substitute.For<IPricingService>();
    private readonly ICustomerLedgerRepository _ledger = Substitute.For<ICustomerLedgerRepository>();

    private readonly CreateDealerOrderHandler _sut;

    public CreateDealerOrderHandlerTests()
    {
        _tenant.RequireTenantId().Returns(TenantId);
        _currentUser.UserIdOrThrow().Returns(UserId);
        _scope.GetCurrentDealerAccountIdAsync(Arg.Any<CancellationToken>()).Returns(DealerAccountId);

        var dealerMembership = new DealerUser(UserId, DealerAccountId, DealerMembershipRole.DealerOwner, null)
        {
            Id = DealerUserId,
            TenantId = TenantId,
        };
        _dealerUsers.GetByUserAndDealerAsync(UserId, DealerAccountId, Arg.Any<CancellationToken>()).Returns(dealerMembership);

        var dealer = new DealerAccount("BAYI", "Demo Bayi", createdByUserId: null) { Id = DealerAccountId, TenantId = TenantId };
        _dealers.GetByIdAsync(DealerAccountId, Arg.Any<CancellationToken>()).Returns(dealer);

        _sequences.ConsumeAsync(DocumentSequenceType.OrderNumber, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("ORD-2026-0001");

        _pricing.ResolveAsync(Arg.Any<PriceResolutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var req = call.Arg<PriceResolutionRequest>();
                return new PriceResolutionResult(
                    UnitPrice: 100m,
                    Currency: req.RequestedCurrency ?? "TRY",
                    DiscountPercent: 0m,
                    Source: PriceSource.ProductListPrice,
                    SourceLabel: "list",
                    ReferenceListPrice: 100m,
                    TaxRatePercent: 0m,
                    IsTaxInclusive: false,
                    TaxRateId: null,
                    AppliedRecordId: null);
            });

        _sut = new CreateDealerOrderHandler(
            _scope, _dealerUsers, _tenant, _currentUser, _orders, _customers, _products, _addresses,
            _paymentTerms, _sequences, _uow, _outbox, _dealers, _pricing,
            new CoreAlign.Application.CustomerPortal.Credit.CreditLimitGuard(_ledger));
    }

    [Fact]
    public async Task HappyPath_creates_order_with_dealer_origin_and_pending_approval()
    {
        SetupAuthorizedCustomerAndProduct();

        Order? captured = null;
        await _orders.AddAsync(Arg.Do<Order>(o => captured = o), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(new CreateDealerOrderCommand(
            CustomerId,
            new[] { new DealerOrderLineInput(ProductId, 2m) }), default);

        captured.Should().NotBeNull();
        captured!.OriginPersona.Should().Be(OrderOriginPersona.Dealer);
        captured.OriginDealerAccountId.Should().Be(DealerAccountId);
        captured.OriginDealerUserId.Should().Be(DealerUserId);
        captured.DealerApprovalStatus.Should().Be(DealerOrderApprovalStatuses.PendingCustomerApproval);
        captured.Status.Should().Be(OrderStatus.Draft);
        result.OriginPersona.Should().Be(OrderOriginPersona.Dealer);
        result.DealerApprovalStatus.Should().Be(DealerOrderApprovalStatuses.PendingCustomerApproval);

        await _outbox.Received(1).EnqueueSubmittedForApprovalAsync(
            Arg.Is<DealerOrderSubmittedForApprovalPayload>(p =>
                p.CustomerId == CustomerId
                && p.DealerAccountId == DealerAccountId
                && p.LineCount == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthorized_customer_throws_and_does_not_persist()
    {
        var otherCustomerId = Guid.NewGuid();
        _scope.GetDealerAllowedCustomerIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { CustomerId });

        var act = async () => await _sut.Handle(new CreateDealerOrderCommand(
            otherCustomerId,
            new[] { new DealerOrderLineInput(ProductId, 1m) }), default);

        await act.Should().ThrowAsync<DealerCustomerNotAuthorizedException>();
        await _orders.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().EnqueueSubmittedForApprovalAsync(
            Arg.Any<DealerOrderSubmittedForApprovalPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Empty_lines_throws()
    {
        var act = async () => await _sut.Handle(new CreateDealerOrderCommand(CustomerId, Array.Empty<DealerOrderLineInput>()), default);
        await act.Should().ThrowAsync<InvalidOrderLineException>();
    }

    private void SetupAuthorizedCustomerAndProduct()
    {
        _scope.GetDealerAllowedCustomerIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { CustomerId });

        var customer = new Customer("Acme Holding") { Id = CustomerId, TenantId = TenantId };
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(customer);

        var product = new Product(
            sku: "SKU-A",
            name: "Widget",
            unit: "adet",
            price: 100m,
            currency: "TRY");
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, ProductId);
        typeof(Product).GetProperty(nameof(Product.TenantId))!.SetValue(product, TenantId);
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [ProductId] = product });
    }
}
