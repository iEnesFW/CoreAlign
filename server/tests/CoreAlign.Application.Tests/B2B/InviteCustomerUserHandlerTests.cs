using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class InviteCustomerUserHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerUserRepository _customerUsers = Substitute.For<ICustomerUserRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IB2BAuthorizationService _authz = Substitute.For<IB2BAuthorizationService>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly InviteCustomerUserHandler _sut;

    public InviteCustomerUserHandlerTests()
    {
        _tenant.RequireTenantId().Returns(TenantId);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed");
        _sut = new InviteCustomerUserHandler(_customers, _customerUsers, _users, _roles, _passwordHasher, _authz, _tenant, _uow);
    }

    [Fact]
    public async Task Creates_user_and_membership_when_caller_is_tenant_admin()
    {
        var customerId = Guid.NewGuid();
        var customer = BuildCustomer(customerId);
        _customers.GetByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);
        _authz.CanManageCustomerAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<string>>(), customerId, Arg.Any<CancellationToken>()).Returns(true);
        _users.GetByEmailAsync("new@demo.local", Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _customerUsers.GetByUserAndCustomerAsync(Arg.Any<Guid>(), customerId, Arg.Any<CancellationToken>()).Returns((CustomerUser?)null);

        var command = new InviteCustomerUserCommand(
            CustomerId: customerId,
            Email: "new@demo.local",
            FirstName: "New",
            LastName: "User",
            Role: CustomerMembershipRole.CustomerStaff,
            CurrentUserId: Guid.NewGuid(),
            CurrentUserRoles: new[] { "TenantAdmin" });

        var result = await _sut.Handle(command, default);

        result.Should().NotBeNull();
        result.MembershipRole.Should().Be(CustomerMembershipRole.CustomerStaff);
        await _users.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _customerUsers.Received(1).AddAsync(Arg.Any<CustomerUser>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_caller_is_not_admin_and_not_customer_owner()
    {
        var customerId = Guid.NewGuid();
        var customer = BuildCustomer(customerId);
        _customers.GetByIdAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);
        _authz.CanManageCustomerAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<string>>(), customerId, Arg.Any<CancellationToken>()).Returns(false);

        var command = new InviteCustomerUserCommand(
            CustomerId: customerId,
            Email: "blocked@demo.local",
            FirstName: null,
            LastName: null,
            Role: CustomerMembershipRole.CustomerStaff,
            CurrentUserId: Guid.NewGuid(),
            CurrentUserRoles: Array.Empty<string>());

        var act = async () => await _sut.Handle(command, default);

        await act.Should().ThrowAsync<B2BForbiddenException>();
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _customerUsers.DidNotReceive().AddAsync(Arg.Any<CustomerUser>(), Arg.Any<CancellationToken>());
    }

    private Customer BuildCustomer(Guid id)
    {
        var customer = new Customer("Acme Holding");
        typeof(Customer).GetProperty(nameof(Customer.Id))!.SetValue(customer, id);
        typeof(Customer).GetProperty(nameof(Customer.TenantId))!.SetValue(customer, TenantId);
        return customer;
    }
}
