using System.Text.Json;
using CoreAlign.Application.B2B.PortalComments;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class OrderCommentPostedOutboxHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid CommentId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();
    private static readonly Guid AuthorUserId = Guid.NewGuid();
    private static readonly Guid DealerUserA = Guid.NewGuid();
    private static readonly Guid DealerUserB = Guid.NewGuid();
    private static readonly Guid CustomerUserA = Guid.NewGuid();
    private static readonly Guid CustomerUserB = Guid.NewGuid();

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Customer_posted_comment_fans_out_to_active_dealer_users()
    {
        var notifications = Substitute.For<INotificationRepository>();
        var customerUsers = Substitute.For<ICustomerUserRepository>();
        var dealerUsers = Substitute.For<IDealerUserRepository>();
        notifications.ExistsForRecipientAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        dealerUsers.ListByDealerAsync(DealerAccountId, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                BuildDealerUser(DealerUserA, MembershipStatus.Active),
                BuildDealerUser(DealerUserB, MembershipStatus.Active),
                BuildDealerUser(Guid.NewGuid(), MembershipStatus.Suspended),
            });

        var captured = new List<Notification>();
        await notifications.AddIfNotExistsAsync(Arg.Do<Notification>(n => captured.Add(n)), Arg.Any<CancellationToken>());

        var sut = new OrderCommentPostedOutboxHandler(notifications, customerUsers, dealerUsers, Substitute.For<IUserRepository>(), Substitute.For<IEmailService>());
        var payload = JsonSerializer.Serialize(new OrderCommentPostedPayload(
            OrderId, CommentId, AuthorUserId, "customer", "Quick question on the spec.", DealerAccountId, CustomerId), Json);

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        captured.Should().HaveCount(2);
        captured.Select(n => n.RecipientUserId).Should().BeEquivalentTo(new[] { DealerUserA, DealerUserB });
        captured.All(n => n.Type == OrderCommentPostedOutboxHandler.NotificationType).Should().BeTrue();
        captured.All(n => n.EntityType == "Order").Should().BeTrue();
    }

    [Fact]
    public async Task Dealer_posted_comment_fans_out_to_active_customer_users()
    {
        var notifications = Substitute.For<INotificationRepository>();
        var customerUsers = Substitute.For<ICustomerUserRepository>();
        var dealerUsers = Substitute.For<IDealerUserRepository>();
        notifications.ExistsForRecipientAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        customerUsers.ListByCustomerAsync(CustomerId, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                BuildCustomerUser(CustomerUserA, MembershipStatus.Active),
                BuildCustomerUser(CustomerUserB, MembershipStatus.Active),
                BuildCustomerUser(Guid.NewGuid(), MembershipStatus.Archived),
            });

        var captured = new List<Notification>();
        await notifications.AddIfNotExistsAsync(Arg.Do<Notification>(n => captured.Add(n)), Arg.Any<CancellationToken>());

        var sut = new OrderCommentPostedOutboxHandler(notifications, customerUsers, dealerUsers, Substitute.For<IUserRepository>(), Substitute.For<IEmailService>());
        var payload = JsonSerializer.Serialize(new OrderCommentPostedPayload(
            OrderId, CommentId, AuthorUserId, "dealer", "Need approval on the quantity change.", DealerAccountId, CustomerId), Json);

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        captured.Should().HaveCount(2);
        captured.Select(n => n.RecipientUserId).Should().BeEquivalentTo(new[] { CustomerUserA, CustomerUserB });
    }

    [Fact]
    public async Task Customer_comment_with_no_dealer_account_results_in_no_recipients()
    {
        var notifications = Substitute.For<INotificationRepository>();
        var customerUsers = Substitute.For<ICustomerUserRepository>();
        var dealerUsers = Substitute.For<IDealerUserRepository>();

        var sut = new OrderCommentPostedOutboxHandler(notifications, customerUsers, dealerUsers, Substitute.For<IUserRepository>(), Substitute.For<IEmailService>());
        var payload = JsonSerializer.Serialize(new OrderCommentPostedPayload(
            OrderId, CommentId, AuthorUserId, "customer", "hello", null, CustomerId), Json);

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        result.ResultOrError.Should().Be("NoRecipients");
        await notifications.DidNotReceive().AddIfNotExistsAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replay_with_existing_notification_is_idempotent()
    {
        var notifications = Substitute.For<INotificationRepository>();
        var customerUsers = Substitute.For<ICustomerUserRepository>();
        var dealerUsers = Substitute.For<IDealerUserRepository>();

        notifications.ExistsForRecipientAsync(
                DealerUserA, "Order", OrderId, OrderCommentPostedOutboxHandler.NotificationType, Arg.Any<CancellationToken>())
            .Returns(true);

        dealerUsers.ListByDealerAsync(DealerAccountId, Arg.Any<CancellationToken>())
            .Returns(new[] { BuildDealerUser(DealerUserA, MembershipStatus.Active) });

        var sut = new OrderCommentPostedOutboxHandler(notifications, customerUsers, dealerUsers, Substitute.For<IUserRepository>(), Substitute.For<IEmailService>());
        var payload = JsonSerializer.Serialize(new OrderCommentPostedPayload(
            OrderId, CommentId, AuthorUserId, "customer", "hi", DealerAccountId, CustomerId), Json);

        var result = await sut.HandleAsync(payload, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        result.ResultOrError.Should().Be("AlreadyProcessed");
        await notifications.DidNotReceive().AddIfNotExistsAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    private static DealerUser BuildDealerUser(Guid userId, MembershipStatus status)
    {
        var entity = new DealerUser(userId, DealerAccountId, DealerMembershipRole.DealerStaff, null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        if (status == MembershipStatus.Suspended) entity.Suspend("test");
        else if (status == MembershipStatus.Archived) entity.Archive();
        return entity;
    }

    private static CustomerUser BuildCustomerUser(Guid userId, MembershipStatus status)
    {
        var entity = new CustomerUser(userId, CustomerId, CustomerMembershipRole.CustomerStaff, null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        if (status == MembershipStatus.Suspended) entity.Suspend("test");
        else if (status == MembershipStatus.Archived) entity.Archive();
        return entity;
    }
}
