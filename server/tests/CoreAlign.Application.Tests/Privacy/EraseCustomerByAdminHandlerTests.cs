using CoreAlign.Application.B2B;
using CoreAlign.Application.Privacy;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Privacy;

public class EraseCustomerByAdminHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActingUserId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid CustomerMemberUserId = Guid.NewGuid();

    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerUserRepository _customerUsers = Substitute.For<ICustomerUserRepository>();
    private readonly IUserAnonymizer _anonymizer = Substitute.For<IUserAnonymizer>();
    private readonly IDataSubjectRequestLog _audit = Substitute.For<IDataSubjectRequestLog>();
    private readonly IPrivacyHasher _hasher = Substitute.For<IPrivacyHasher>();
    private readonly IPrivacyEraseService _eraseService = Substitute.For<IPrivacyEraseService>();

    [Fact]
    public async Task Anonymizes_customer_and_cascades_children_and_members()
    {
        var actingUser = BuildUser(ActingUserId, "admin", "admin@example.com");
        var customer = BuildCustomer();
        var member = BuildUser(CustomerMemberUserId, "member", "m@example.com");
        var membership = new CustomerUser(member.Id, customer.Id, CustomerMembershipRole.CustomerOwner, ActingUserId)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };

        _currentUser.UserIdOrThrow().Returns(actingUser.Id);
        _users.GetByIdAsync(actingUser.Id, Arg.Any<CancellationToken>()).Returns(actingUser);
        _users.ListByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { member });
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _customerUsers.ListByCustomerAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(new[] { membership });
        _hasher.Hash(Arg.Any<Guid>(), Arg.Any<string?>()).Returns("hash");

        var sut = BuildSut();

        var result = await sut.Handle(new EraseCustomerByAdminCommand(customer.Id, actingUser.Username), default);

        result.UserId.Should().Be(customer.Id);
        customer.IsAnonymized.Should().BeTrue();

        await _anonymizer.Received(1).AnonymizeAsync(member, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _eraseService.Received(1).AnonymizeCustomerChildrenAsync(
            customer.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_confirmation_does_not_match()
    {
        var actingUser = BuildUser(ActingUserId, "admin", "admin@example.com");
        _currentUser.UserIdOrThrow().Returns(actingUser.Id);
        _users.GetByIdAsync(actingUser.Id, Arg.Any<CancellationToken>()).Returns(actingUser);

        var sut = BuildSut();
        Func<Task> act = () => sut.Handle(new EraseCustomerByAdminCommand(CustomerId, "wrong"), default);

        await act.Should().ThrowAsync<PrivacyConfirmationMismatchException>();
    }

    [Fact]
    public async Task Throws_when_customer_already_anonymized()
    {
        var actingUser = BuildUser(ActingUserId, "admin", "admin@example.com");
        var customer = BuildCustomer();
        customer.Anonymize("[Silinmiş Müşteri-1]");

        _currentUser.UserIdOrThrow().Returns(actingUser.Id);
        _users.GetByIdAsync(actingUser.Id, Arg.Any<CancellationToken>()).Returns(actingUser);
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var sut = BuildSut();
        Func<Task> act = () => sut.Handle(new EraseCustomerByAdminCommand(customer.Id, actingUser.Username), default);

        await act.Should().ThrowAsync<KvkkEraseAlreadyProcessedException>();
    }

    [Fact]
    public async Task Throws_when_customer_not_found()
    {
        var actingUser = BuildUser(ActingUserId, "admin", "admin@example.com");
        _currentUser.UserIdOrThrow().Returns(actingUser.Id);
        _users.GetByIdAsync(actingUser.Id, Arg.Any<CancellationToken>()).Returns(actingUser);
        _customers.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var sut = BuildSut();
        Func<Task> act = () => sut.Handle(new EraseCustomerByAdminCommand(Guid.NewGuid(), actingUser.Username), default);

        await act.Should().ThrowAsync<PrivacyCustomerNotFoundException>();
    }

    [Fact]
    public async Task Writes_audit_log_entries_for_customer_and_members()
    {
        var actingUser = BuildUser(ActingUserId, "admin", "admin@example.com");
        var customer = BuildCustomer();
        var member = BuildUser(CustomerMemberUserId, "member", "m@example.com");
        var membership = new CustomerUser(member.Id, customer.Id, CustomerMembershipRole.CustomerOwner, ActingUserId)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };

        _currentUser.UserIdOrThrow().Returns(actingUser.Id);
        _users.GetByIdAsync(actingUser.Id, Arg.Any<CancellationToken>()).Returns(actingUser);
        _users.ListByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { member });
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _customerUsers.ListByCustomerAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(new[] { membership });
        _hasher.Hash(Arg.Any<Guid>(), Arg.Any<string?>()).Returns("hash");

        var sut = BuildSut();

        await sut.Handle(new EraseCustomerByAdminCommand(customer.Id, actingUser.Username), default);

        await _audit.Received().RecordErasureAsync(
            customer.TenantId,
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    private EraseCustomerByAdminHandler BuildSut() =>
        new(_currentUser, _tenant, _users, _customers, _customerUsers, _anonymizer, _audit, _hasher, _eraseService);

    private static Customer BuildCustomer()
    {
        var customer = new Customer("Acme", CustomerType.Business, taxNumber: "1234567890", email: "info@acme.test")
        {
            Id = CustomerId,
            TenantId = TenantId,
        };
        return customer;
    }

    private static User BuildUser(Guid id, string username, string email)
    {
        var user = new User(TenantId, username, email, "hash") { Id = id };
        user.NormalizedEmail = email.ToUpperInvariant();
        return user;
    }
}
