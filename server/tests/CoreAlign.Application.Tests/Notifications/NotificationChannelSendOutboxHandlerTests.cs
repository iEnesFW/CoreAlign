using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Notifications.Delivery;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Application.Providers;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Notifications;

public class NotificationChannelSendOutboxHandlerTests
{
    private readonly INotificationMessageRepository _messages = Substitute.For<INotificationMessageRepository>();
    private readonly IProviderRegistry<IEmailProvider> _emailRegistry = Substitute.For<IProviderRegistry<IEmailProvider>>();
    private readonly IProviderRegistry<ISmsProvider> _smsRegistry = Substitute.For<IProviderRegistry<ISmsProvider>>();
    private readonly IProviderRegistry<IPushNotificationProvider> _pushRegistry = Substitute.For<IProviderRegistry<IPushNotificationProvider>>();
    private readonly IProviderRegistry<IWhatsAppProvider> _whatsAppRegistry = Substitute.For<IProviderRegistry<IWhatsAppProvider>>();
    private readonly IUserDeviceTokenRepository _deviceTokens = Substitute.For<IUserDeviceTokenRepository>();
    private readonly INotificationRateLimiter _rateLimiter = Substitute.For<INotificationRateLimiter>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEmailProvider _emailProvider = Substitute.For<IEmailProvider>();

    private readonly Guid _tenantId = Guid.NewGuid();

    private NotificationChannelSendOutboxHandler BuildSut() =>
        new(
            _messages,
            _emailRegistry,
            _smsRegistry,
            _pushRegistry,
            _whatsAppRegistry,
            _deviceTokens,
            _rateLimiter,
            _unitOfWork,
            NullLogger<NotificationChannelSendOutboxHandler>.Instance);

    private NotificationMessage BuildMessage()
    {
        var message = new NotificationMessage(
            _tenantId, null, null, NotificationChannel.Email, "Warranty.Activated", "tr",
            "user@example.com", "Warranty", "Subject", "<p>Body</p>", "{}", "hash-1") { Id = Guid.NewGuid() };
        message.MarkQueued(DateTime.UtcNow);
        return message;
    }

    private static string Payload(Guid tenantId, Guid messageId) =>
        JsonSerializer.Serialize(new NotificationChannelSendPayload(
            tenantId, messageId, NotificationChannel.Email, "user@example.com", "Subject", "<p>Body</p>", "Body"));

    private void Allowed() =>
        _rateLimiter.TryAcquireAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RateDecision(true, DateTime.UtcNow.AddMinutes(1), null));

    [Fact]
    public async Task Sends_email_and_marks_message_sent()
    {
        var message = BuildMessage();
        _messages.GetByIdAsync(_tenantId, message.Id, Arg.Any<CancellationToken>()).Returns(message);
        _emailRegistry.TryResolveForTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(_emailProvider);
        _emailProvider.Name.Returns("smtp");
        _emailProvider.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()).Returns(NotificationSendResult.Ok("msg-1"));
        Allowed();

        var result = await BuildSut().HandleAsync(Payload(_tenantId, message.Id), CancellationToken.None);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        message.Status.Should().Be(NotificationStatus.Sent);
        message.ProviderMessageId.Should().Be("msg-1");
    }

    [Fact]
    public async Task Provider_failure_marks_message_failed_and_returns_failed()
    {
        var message = BuildMessage();
        _messages.GetByIdAsync(_tenantId, message.Id, Arg.Any<CancellationToken>()).Returns(message);
        _emailRegistry.TryResolveForTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(_emailProvider);
        _emailProvider.Name.Returns("smtp");
        _emailProvider.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()).Returns(NotificationSendResult.Fail("smtp timeout"));
        Allowed();

        var result = await BuildSut().HandleAsync(Payload(_tenantId, message.Id), CancellationToken.None);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Failed);
        message.Status.Should().Be(NotificationStatus.Failed);
        message.FailureReason.Should().Be("smtp timeout");
        message.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task Already_sent_message_is_idempotently_skipped()
    {
        var message = BuildMessage();
        message.MarkSent("smtp", "msg-prev", DateTime.UtcNow);
        _messages.GetByIdAsync(_tenantId, message.Id, Arg.Any<CancellationToken>()).Returns(message);

        var result = await BuildSut().HandleAsync(Payload(_tenantId, message.Id), CancellationToken.None);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        await _emailProvider.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rate_limited_send_defers_with_retry_window()
    {
        var message = BuildMessage();
        _messages.GetByIdAsync(_tenantId, message.Id, Arg.Any<CancellationToken>()).Returns(message);
        _emailRegistry.TryResolveForTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(_emailProvider);
        _emailProvider.Name.Returns("smtp");
        var windowEnd = DateTime.UtcNow.AddMinutes(1);
        _rateLimiter.TryAcquireAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RateDecision(false, windowEnd, "Recipient send rate limit reached"));

        var result = await BuildSut().HandleAsync(Payload(_tenantId, message.Id), CancellationToken.None);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Deferred);
        result.RetryAfterUtc.Should().Be(windowEnd);
        message.Status.Should().Be(NotificationStatus.Queued);
        await _emailProvider.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
