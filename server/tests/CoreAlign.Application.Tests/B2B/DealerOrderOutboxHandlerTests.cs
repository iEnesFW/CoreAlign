using System.Text.Json;
using CoreAlign.Application.B2B.DealerOrderFlow;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class DealerOrderOutboxHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();
    private static readonly Guid DealerUserId = Guid.NewGuid();
    private static readonly Guid CustomerUser1 = Guid.NewGuid();
    private static readonly Guid CustomerUser2 = Guid.NewGuid();

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Submitted_handler_creates_one_notification_per_active_customer_user()
    {
        var notifications = Substitute.For<INotificationRepository>();
        var customerUsers = Substitute.For<ICustomerUserRepository>();
        notifications.ExistsForRecipientAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var members = new[]
        {
            BuildCustomerUser(CustomerUser1, MembershipStatus.Active),
            BuildCustomerUser(CustomerUser2, MembershipStatus.Active),
            BuildCustomerUser(Guid.NewGuid(), MembershipStatus.Suspended),
        };
        customerUsers.ListByCustomerAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(members);

        var captured = new List<Notification>();
        await notifications.AddIfNotExistsAsync(Arg.Do<Notification>(n => captured.Add(n)), Arg.Any<CancellationToken>());

        var sut = new DealerOrderSubmittedForApprovalOutboxHandler(notifications, customerUsers, Substitute.For<IUserRepository>(), Substitute.For<IEmailService>());
        var payload = JsonSerializer.Serialize(new DealerOrderSubmittedForApprovalPayload(
            OrderId, TenantId, CustomerId, DealerAccountId, "Demo Bayi", 2, 200m, "TRY", DealerUserId), Json);

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        captured.Should().HaveCount(2);
        captured.All(n => n.EntityId == OrderId).Should().BeTrue();
        captured.All(n => n.Type == DealerOrderSubmittedForApprovalOutboxHandler.NotificationType).Should().BeTrue();
    }

    [Fact]
    public async Task Submitted_handler_replay_is_idempotent()
    {
        var notifications = Substitute.For<INotificationRepository>();
        var customerUsers = Substitute.For<ICustomerUserRepository>();

        notifications.ExistsForRecipientAsync(CustomerUser1, "Order", OrderId,
                DealerOrderSubmittedForApprovalOutboxHandler.NotificationType, Arg.Any<CancellationToken>())
            .Returns(true);

        customerUsers.ListByCustomerAsync(CustomerId, Arg.Any<CancellationToken>())
            .Returns(new[] { BuildCustomerUser(CustomerUser1, MembershipStatus.Active) });

        var sut = new DealerOrderSubmittedForApprovalOutboxHandler(notifications, customerUsers, Substitute.For<IUserRepository>(), Substitute.For<IEmailService>());
        var payload = JsonSerializer.Serialize(new DealerOrderSubmittedForApprovalPayload(
            OrderId, TenantId, CustomerId, DealerAccountId, "Demo Bayi", 1, 100m, "TRY", DealerUserId), Json);

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        result.ResultOrError.Should().Be("AlreadyProcessed");
        await notifications.DidNotReceive().AddIfNotExistsAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approved_handler_notifies_dealer_user_and_tenant_admins()
    {
        var notifications = Substitute.For<INotificationRepository>();
        var users = Substitute.For<IUserRepository>();
        var roles = Substitute.For<IRoleRepository>();

        notifications.ExistsForRecipientAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var adminRole = new Role { Id = 1, Name = "TenantAdmin" };
        roles.GetByNameAsync("TenantAdmin", Arg.Any<CancellationToken>()).Returns(adminRole);

        var adminUser = BuildUserWithRole(adminRole.Id, isActive: true);
        var staffUser = BuildUserWithRole(roleId: 99, isActive: true);
        users.ListByTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns(new[] { adminUser, staffUser });

        var captured = new List<Notification>();
        await notifications.AddIfNotExistsAsync(Arg.Do<Notification>(n => captured.Add(n)), Arg.Any<CancellationToken>());

        var sut = new DealerOrderApprovedByCustomerOutboxHandler(notifications, users, roles);
        var payload = JsonSerializer.Serialize(new DealerOrderApprovedByCustomerPayload(
            OrderId, TenantId, CustomerId, "Acme", DealerAccountId, "Bayi", DealerUserId, Guid.NewGuid(), 2, 200m, "TRY"), Json);

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        var recipients = captured.Select(n => n.RecipientUserId).ToHashSet();
        recipients.Should().Contain(DealerUserId);
        recipients.Should().Contain(adminUser.Id);
        recipients.Should().NotContain(staffUser.Id);
    }

    [Fact]
    public async Task Submitted_handler_called_twice_shortcircuits_second_call_via_exists_check()
    {
        var notifications = Substitute.For<INotificationRepository>();
        var customerUsers = Substitute.For<ICustomerUserRepository>();

        var existing = new HashSet<Guid>();
        notifications.ExistsForRecipientAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => existing.Contains((Guid)call[0]));

        await notifications.AddIfNotExistsAsync(Arg.Do<Notification>(n => existing.Add(n.RecipientUserId)), Arg.Any<CancellationToken>());

        customerUsers.ListByCustomerAsync(CustomerId, Arg.Any<CancellationToken>())
            .Returns(new[] { BuildCustomerUser(CustomerUser1, MembershipStatus.Active) });

        var sut = new DealerOrderSubmittedForApprovalOutboxHandler(notifications, customerUsers, Substitute.For<IUserRepository>(), Substitute.For<IEmailService>());
        var payload = JsonSerializer.Serialize(new DealerOrderSubmittedForApprovalPayload(
            OrderId, TenantId, CustomerId, DealerAccountId, "Demo Bayi", 1, 100m, "TRY", DealerUserId), Json);

        var first = await sut.HandleAsync(payload, default);
        var second = await sut.HandleAsync(payload, default);

        first.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        first.ResultOrError.Should().Be("FannedOut:1");

        second.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        second.ResultOrError.Should().Be("AlreadyProcessed");

        await notifications.Received(1).AddIfNotExistsAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejected_handler_notifies_dealer_user_with_reason()
    {
        var notifications = Substitute.For<INotificationRepository>();
        notifications.ExistsForRecipientAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Notification? captured = null;
        await notifications.AddIfNotExistsAsync(Arg.Do<Notification>(n => captured = n), Arg.Any<CancellationToken>());

        var sut = new DealerOrderRejectedByCustomerOutboxHandler(notifications);
        var payload = JsonSerializer.Serialize(new DealerOrderRejectedByCustomerPayload(
            OrderId, TenantId, CustomerId, "Acme", DealerAccountId, "Bayi", DealerUserId, Guid.NewGuid(),
            "Fiyat çok yüksek geldi"), Json);

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        captured.Should().NotBeNull();
        captured!.RecipientUserId.Should().Be(DealerUserId);
        captured.Body.Should().Contain("Fiyat çok yüksek geldi");
    }

    private static CustomerUser BuildCustomerUser(Guid userId, MembershipStatus status)
    {
        var cu = new CustomerUser(userId, CustomerId, CustomerMembershipRole.CustomerStaff, null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        if (status == MembershipStatus.Suspended) cu.Suspend("test");
        else if (status == MembershipStatus.Archived) cu.Archive();
        return cu;
    }

    private static User BuildUserWithRole(int roleId, bool isActive)
    {
        var user = new User(TenantId, "u" + Guid.NewGuid().ToString("N").Substring(0, 6), $"u{Guid.NewGuid():N}@x.test", "hash")
        {
            IsActive = isActive,
        };
        user.UserRoles.Add(new UserRole(user.Id, roleId));
        return user;
    }
}
