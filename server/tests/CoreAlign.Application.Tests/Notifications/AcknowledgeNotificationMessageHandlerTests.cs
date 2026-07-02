using CoreAlign.Application.Notifications.Messages;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Notifications;

public class AcknowledgeNotificationMessageHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid RecipientId = Guid.NewGuid();

    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly INotificationMessageRepository _messages = Substitute.For<INotificationMessageRepository>();
    private readonly AcknowledgeNotificationMessageHandler _sut;

    public AcknowledgeNotificationMessageHandlerTests()
    {
        _tenantContext.RequireTenantId().Returns(TenantId);
        _sut = new AcknowledgeNotificationMessageHandler(_tenantContext, _messages);
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
    public async Task Acknowledging_own_notification_sets_note_and_actor()
    {
        var message = BuildMessage(RecipientId);
        _messages.GetByIdAsync(TenantId, message.Id, Arg.Any<CancellationToken>()).Returns(message);

        await _sut.Handle(
            new AcknowledgeNotificationMessageCommand(message.Id, "  Looks good  ", RecipientId),
            CancellationToken.None);

        message.IsAcknowledged.Should().BeTrue();
        message.AcknowledgmentNote.Should().Be("Looks good");
        message.AcknowledgedByUserId.Should().Be(RecipientId);
        message.AcknowledgedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Acknowledging_another_users_notification_throws_forbidden()
    {
        var message = BuildMessage(Guid.NewGuid());
        _messages.GetByIdAsync(TenantId, message.Id, Arg.Any<CancellationToken>()).Returns(message);

        var act = () => _sut.Handle(
            new AcknowledgeNotificationMessageCommand(message.Id, "note", RecipientId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotificationMessageAccessForbiddenException>();
        message.IsAcknowledged.Should().BeFalse();
    }

    [Fact]
    public async Task Acknowledging_missing_notification_throws_not_found()
    {
        var id = Guid.NewGuid();
        _messages.GetByIdAsync(TenantId, id, Arg.Any<CancellationToken>()).Returns((NotificationMessage?)null);

        var act = () => _sut.Handle(
            new AcknowledgeNotificationMessageCommand(id, null, RecipientId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotificationMessageNotFoundException>();
    }

    [Fact]
    public async Task Re_acknowledging_replaces_the_note()
    {
        var message = BuildMessage(RecipientId);
        _messages.GetByIdAsync(TenantId, message.Id, Arg.Any<CancellationToken>()).Returns(message);

        await _sut.Handle(new AcknowledgeNotificationMessageCommand(message.Id, "first", RecipientId), CancellationToken.None);
        await _sut.Handle(new AcknowledgeNotificationMessageCommand(message.Id, "second", RecipientId), CancellationToken.None);

        message.AcknowledgmentNote.Should().Be("second");
    }
}
