using CoreAlign.Application.Notifications.Messages;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Notifications;

public class MarkNotificationMessageReadHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid RecipientId = Guid.NewGuid();

    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly INotificationMessageRepository _messages = Substitute.For<INotificationMessageRepository>();
    private readonly MarkNotificationMessageReadHandler _sut;

    public MarkNotificationMessageReadHandlerTests()
    {
        _tenantContext.RequireTenantId().Returns(TenantId);
        _sut = new MarkNotificationMessageReadHandler(_tenantContext, _messages);
    }

    private static NotificationMessage BuildMessage(Guid userId) => new(
        TenantId,
        userId,
        customerId: null,
        NotificationChannel.InApp,
        "Dunning.InvoiceDueReminder",
        "tr",
        recipientAddress: userId.ToString("N"),
        categoryKey: "Dunning",
        subject: "Subject",
        bodyMarkdown: "Body",
        payloadJson: "{}");

    [Fact]
    public async Task Marking_own_notification_read_sets_status_and_timestamp()
    {
        var message = BuildMessage(RecipientId);
        _messages.GetByIdAsync(TenantId, message.Id, Arg.Any<CancellationToken>()).Returns(message);

        await _sut.Handle(
            new MarkNotificationMessageReadCommand(message.Id, RecipientId),
            CancellationToken.None);

        message.Status.Should().Be(NotificationStatus.Read);
        message.ReadAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Marking_another_users_notification_read_throws_forbidden()
    {
        var message = BuildMessage(Guid.NewGuid());
        _messages.GetByIdAsync(TenantId, message.Id, Arg.Any<CancellationToken>()).Returns(message);

        var act = () => _sut.Handle(
            new MarkNotificationMessageReadCommand(message.Id, RecipientId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotificationMessageAccessForbiddenException>();
        message.Status.Should().NotBe(NotificationStatus.Read);
        message.ReadAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Marking_missing_notification_read_throws_not_found()
    {
        var id = Guid.NewGuid();
        _messages.GetByIdAsync(TenantId, id, Arg.Any<CancellationToken>()).Returns((NotificationMessage?)null);

        var act = () => _sut.Handle(
            new MarkNotificationMessageReadCommand(id, RecipientId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotificationMessageNotFoundException>();
    }

    [Fact]
    public async Task Re_marking_read_is_idempotent()
    {
        var message = BuildMessage(RecipientId);
        _messages.GetByIdAsync(TenantId, message.Id, Arg.Any<CancellationToken>()).Returns(message);

        await _sut.Handle(new MarkNotificationMessageReadCommand(message.Id, RecipientId), CancellationToken.None);
        var firstReadAt = message.ReadAtUtc;
        await _sut.Handle(new MarkNotificationMessageReadCommand(message.Id, RecipientId), CancellationToken.None);

        message.Status.Should().Be(NotificationStatus.Read);
        message.ReadAtUtc.Should().Be(firstReadAt);
    }
}
