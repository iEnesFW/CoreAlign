using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class CreateDealerAccountHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly IDealerAccountRepository _dealers = Substitute.For<IDealerAccountRepository>();
    private readonly IDealerCustomerLinkRepository _links = Substitute.For<IDealerCustomerLinkRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IB2BAuthorizationService _authz = Substitute.For<IB2BAuthorizationService>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CreateDealerAccountHandler _sut;

    public CreateDealerAccountHandlerTests()
    {
        _tenant.RequireTenantId().Returns(TenantId);
        _sut = new CreateDealerAccountHandler(_dealers, _links, _customers, _authz, _tenant, _uow);
    }

    [Fact]
    public async Task CustomerOwner_creating_dealer_auto_links_to_callers_customer()
    {
        var callerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var customer = BuildCustomer(customerId);
        _customers.GetByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);
        _authz.IsCustomerOwnerAsync(callerId, customerId, Arg.Any<CancellationToken>()).Returns(true);
        _dealers.CodeExistsAsync("BAYI-NEW", null, Arg.Any<CancellationToken>()).Returns(false);

        DealerCustomerLink? capturedLink = null;
        await _links.AddAsync(Arg.Do<DealerCustomerLink>(l => capturedLink = l), Arg.Any<CancellationToken>());

        var command = new CreateDealerAccountCommand(
            Code: "BAYI-NEW",
            Name: "Yeni Bayi",
            PrimaryCustomerId: customerId,
            CurrentUserId: callerId,
            CurrentUserRoles: Array.Empty<string>());

        var result = await _sut.Handle(command, default);

        result.Code.Should().Be("BAYI-NEW");
        result.Name.Should().Be("Yeni Bayi");
        await _dealers.Received(1).AddAsync(Arg.Any<DealerAccount>(), Arg.Any<CancellationToken>());
        await _links.Received(1).AddAsync(Arg.Any<DealerCustomerLink>(), Arg.Any<CancellationToken>());
        capturedLink.Should().NotBeNull();
        capturedLink!.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task NonAdmin_without_primary_customer_is_forbidden()
    {
        var command = new CreateDealerAccountCommand(
            Code: "BAYI-X",
            Name: "Bağımsız Bayi",
            PrimaryCustomerId: null,
            CurrentUserId: Guid.NewGuid(),
            CurrentUserRoles: Array.Empty<string>());

        var act = async () => await _sut.Handle(command, default);

        await act.Should().ThrowAsync<B2BForbiddenException>();
        await _dealers.DidNotReceive().AddAsync(Arg.Any<DealerAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_code_throws_DuplicateDealerCodeException()
    {
        _dealers.CodeExistsAsync("DUPE", null, Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateDealerAccountCommand(
            Code: "DUPE",
            Name: "Dupe Bayi",
            PrimaryCustomerId: null,
            CurrentUserId: Guid.NewGuid(),
            CurrentUserRoles: new[] { "TenantAdmin" });

        var act = async () => await _sut.Handle(command, default);

        await act.Should().ThrowAsync<DuplicateDealerCodeException>();
    }

    private Customer BuildCustomer(Guid id)
    {
        var customer = new Customer("Acme Holding");
        typeof(Customer).GetProperty(nameof(Customer.Id))!.SetValue(customer, id);
        typeof(Customer).GetProperty(nameof(Customer.TenantId))!.SetValue(customer, TenantId);
        return customer;
    }
}
