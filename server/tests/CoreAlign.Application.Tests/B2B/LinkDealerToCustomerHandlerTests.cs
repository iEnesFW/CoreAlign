using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class LinkDealerToCustomerHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly IDealerCustomerLinkRepository _links = Substitute.For<IDealerCustomerLinkRepository>();
    private readonly IDealerAccountRepository _dealers = Substitute.For<IDealerAccountRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IB2BAuthorizationService _authz = Substitute.For<IB2BAuthorizationService>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly LinkDealerToCustomerHandler _sut;

    public LinkDealerToCustomerHandlerTests()
    {
        _tenant.RequireTenantId().Returns(TenantId);
        _sut = new LinkDealerToCustomerHandler(_links, _dealers, _customers, _authz, _tenant, _uow);
    }

    [Fact]
    public async Task Reactivates_existing_suspended_link_instead_of_creating_duplicate()
    {
        var dealerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var dealer = BuildDealer(dealerId);
        var customer = BuildCustomer(customerId);
        var existingLink = new DealerCustomerLink(dealerId, customerId, assignedByUserId: null);
        typeof(TenantEntityHelper).GetMethod("SetTenant")!.Invoke(null, new object[] { existingLink, TenantId });
        existingLink.Suspend();

        _dealers.GetByIdAsync(dealerId, Arg.Any<CancellationToken>()).Returns(dealer);
        _customers.GetByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);
        _authz.CanManageCustomerAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<string>>(), customerId, Arg.Any<CancellationToken>()).Returns(true);
        _links.GetByDealerAndCustomerAsync(dealerId, customerId, Arg.Any<CancellationToken>()).Returns(existingLink);

        var command = new LinkDealerToCustomerCommand(
            DealerAccountId: dealerId,
            CustomerId: customerId,
            Notes: null,
            CurrentUserId: Guid.NewGuid(),
            CurrentUserRoles: new[] { "TenantAdmin" });

        var result = await _sut.Handle(command, default);

        result.Status.Should().Be(DealerCustomerLinkStatus.Active);
        existingLink.Status.Should().Be(DealerCustomerLinkStatus.Active);
        await _links.DidNotReceive().AddAsync(Arg.Any<DealerCustomerLink>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_active_link_when_already_active_idempotent()
    {
        var dealerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var dealer = BuildDealer(dealerId);
        var customer = BuildCustomer(customerId);
        var existingLink = new DealerCustomerLink(dealerId, customerId, assignedByUserId: null);
        typeof(TenantEntityHelper).GetMethod("SetTenant")!.Invoke(null, new object[] { existingLink, TenantId });

        _dealers.GetByIdAsync(dealerId, Arg.Any<CancellationToken>()).Returns(dealer);
        _customers.GetByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);
        _authz.CanManageCustomerAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<string>>(), customerId, Arg.Any<CancellationToken>()).Returns(true);
        _links.GetByDealerAndCustomerAsync(dealerId, customerId, Arg.Any<CancellationToken>()).Returns(existingLink);

        var command = new LinkDealerToCustomerCommand(
            DealerAccountId: dealerId,
            CustomerId: customerId,
            Notes: null,
            CurrentUserId: Guid.NewGuid(),
            CurrentUserRoles: new[] { "TenantAdmin" });

        var result = await _sut.Handle(command, default);

        result.Status.Should().Be(DealerCustomerLinkStatus.Active);
        await _links.DidNotReceive().AddAsync(Arg.Any<DealerCustomerLink>(), Arg.Any<CancellationToken>());
        _links.DidNotReceive().Update(Arg.Any<DealerCustomerLink>());
    }

    private Customer BuildCustomer(Guid id)
    {
        var customer = new Customer("Acme Holding");
        typeof(Customer).GetProperty(nameof(Customer.Id))!.SetValue(customer, id);
        typeof(Customer).GetProperty(nameof(Customer.TenantId))!.SetValue(customer, TenantId);
        return customer;
    }

    private DealerAccount BuildDealer(Guid id)
    {
        var dealer = new DealerAccount("BAYI-01", "Demo Bayi", null);
        typeof(DealerAccount).GetProperty(nameof(DealerAccount.Id))!.SetValue(dealer, id);
        typeof(DealerAccount).GetProperty(nameof(DealerAccount.TenantId))!.SetValue(dealer, TenantId);
        return dealer;
    }
}

internal static class TenantEntityHelper
{
    public static void SetTenant(CoreAlign.Domain.Common.TenantEntity entity, Guid tenantId) =>
        entity.TenantId = tenantId;
}
