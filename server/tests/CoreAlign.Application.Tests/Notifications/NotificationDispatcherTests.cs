using CoreAlign.Application.Notifications;
using CoreAlign.Application.Notifications.Delivery;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Application.Notifications.Templates;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Notifications;

public class NotificationDispatcherTests
{
    private readonly INotificationTemplateRenderer _renderer = Substitute.For<INotificationTemplateRenderer>();
    private readonly INotificationMessageRepository _messages = Substitute.For<INotificationMessageRepository>();
    private readonly INotificationPreferenceRepository _preferences = Substitute.For<INotificationPreferenceRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUserDeviceTokenRepository _deviceTokens = Substitute.For<IUserDeviceTokenRepository>();
    private readonly INotificationDeliveryQueue _deliveryQueue = Substitute.For<INotificationDeliveryQueue>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserConsentRepository _consents = Substitute.For<IUserConsentRepository>();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private NotificationDispatcher BuildSut() =>
        new(
            _renderer,
            _messages,
            _preferences,
            _users,
            _customers,
            _deviceTokens,
            _deliveryQueue,
            _unitOfWork,
            _consents,
            NullLogger<NotificationDispatcher>.Instance);

    private NotificationRequest BuildRequest(IReadOnlyList<NotificationChannel>? channels = null) =>
        new(
            TenantId: _tenantId,
            UserId: _userId,
            CustomerId: null,
            CategoryKey: "Warranty",
            TemplateKey: "Warranty.Activated",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["warrantyNumber"] = "W-1",
            },
            ChannelsOverride: channels,
            RecipientEmailOverride: "user@example.com",
            RecipientPhoneOverride: "+905551234567");

    private void StubTemplateRender()
    {
        _renderer
            .RenderAsync(Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<NotificationChannel>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new RenderedTemplate("Subject", "<p>Body</p>", "Body"));
    }

    [Fact]
    public async Task DispatchAsync_queues_email_channel_without_calling_provider()
    {
        StubTemplateRender();
        var sut = BuildSut();

        var results = await sut.DispatchAsync(BuildRequest(new[] { NotificationChannel.Email }));

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        await _messages.Received(1).UpsertAsync(
            Arg.Is<NotificationMessage>(m =>
                m.TenantId == _tenantId &&
                m.Channel == NotificationChannel.Email &&
                m.Status == NotificationStatus.Queued &&
                m.RecipientAddress == "user@example.com"),
            Arg.Any<CancellationToken>());
        await _deliveryQueue.Received(1).EnqueueChannelSendAsync(
            Arg.Is<NotificationChannelSendPayload>(p =>
                p.Channel == NotificationChannel.Email &&
                p.Address == "user@example.com" &&
                p.Subject == "Subject"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_marks_inapp_sent_inline_without_queue()
    {
        StubTemplateRender();
        var sut = BuildSut();

        var results = await sut.DispatchAsync(BuildRequest(new[] { NotificationChannel.InApp }));

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        await _messages.Received(1).UpsertAsync(
            Arg.Is<NotificationMessage>(m => m.Channel == NotificationChannel.InApp && m.Status == NotificationStatus.Sent),
            Arg.Any<CancellationToken>());
        await _deliveryQueue.DidNotReceive().EnqueueChannelSendAsync(Arg.Any<NotificationChannelSendPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_skips_channel_when_user_opted_out()
    {
        StubTemplateRender();
        var disabledPref = new NotificationPreference(_tenantId, _userId, "Warranty", NotificationChannel.Email, false);
        _preferences.GetAsync(_tenantId, _userId, "Warranty", NotificationChannel.Email, Arg.Any<CancellationToken>())
            .Returns(disabledPref);

        var sut = BuildSut();

        var results = await sut.DispatchAsync(BuildRequest(new[] { NotificationChannel.Email }));

        results.Should().BeEmpty();
        await _deliveryQueue.DidNotReceive().EnqueueChannelSendAsync(Arg.Any<NotificationChannelSendPayload>(), Arg.Any<CancellationToken>());
        await _messages.DidNotReceive().UpsertAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_push_resolves_tokens_and_enqueues_per_token()
    {
        StubTemplateRender();
        var tokenA = new UserDeviceToken(_tenantId, _userId, "token-a", "ios", "iPhone", "17.0", DateTime.UtcNow);
        var tokenB = new UserDeviceToken(_tenantId, _userId, "token-b", "android", "Pixel", "14", DateTime.UtcNow);
        _deviceTokens.ListActiveByUserAsync(_tenantId, _userId, Arg.Any<CancellationToken>())
            .Returns(new List<UserDeviceToken> { tokenA, tokenB });

        var sut = BuildSut();
        var request = new NotificationRequest(
            TenantId: _tenantId,
            UserId: _userId,
            CustomerId: null,
            CategoryKey: "Warranty",
            TemplateKey: "Warranty.Activated",
            Locale: "tr",
            Payload: new Dictionary<string, object?> { ["warrantyNumber"] = "W-1" },
            ChannelsOverride: new[] { NotificationChannel.Push });

        var results = await sut.DispatchAsync(request);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Success);
        await _deliveryQueue.Received(1).EnqueueChannelSendAsync(
            Arg.Is<NotificationChannelSendPayload>(p => p.Channel == NotificationChannel.Push && p.Address == "token-a"),
            Arg.Any<CancellationToken>());
        await _deliveryQueue.Received(1).EnqueueChannelSendAsync(
            Arg.Is<NotificationChannelSendPayload>(p => p.Channel == NotificationChannel.Push && p.Address == "token-b"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_push_returns_NoDeviceTokens_when_repository_empty_and_no_override()
    {
        StubTemplateRender();
        _deviceTokens.ListActiveByUserAsync(_tenantId, _userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UserDeviceToken>());

        var sut = BuildSut();
        var request = new NotificationRequest(
            TenantId: _tenantId,
            UserId: _userId,
            CustomerId: null,
            CategoryKey: "Warranty",
            TemplateKey: "Warranty.Activated",
            Locale: "tr",
            Payload: new Dictionary<string, object?>(),
            ChannelsOverride: new[] { NotificationChannel.Push });

        var results = await sut.DispatchAsync(request);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
        results[0].FailureReason.Should().Be("NoDeviceTokens");
        await _deliveryQueue.DidNotReceive().EnqueueChannelSendAsync(Arg.Any<NotificationChannelSendPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_push_uses_override_token_when_provided()
    {
        StubTemplateRender();
        var sut = BuildSut();
        var request = new NotificationRequest(
            TenantId: _tenantId,
            UserId: _userId,
            CustomerId: null,
            CategoryKey: "Warranty",
            TemplateKey: "Warranty.Activated",
            Locale: "tr",
            Payload: new Dictionary<string, object?>(),
            ChannelsOverride: new[] { NotificationChannel.Push },
            RecipientDeviceTokenOverride: "override-token");

        var results = await sut.DispatchAsync(request);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        await _deliveryQueue.Received(1).EnqueueChannelSendAsync(
            Arg.Is<NotificationChannelSendPayload>(p => p.Address == "override-token"),
            Arg.Any<CancellationToken>());
        await _deviceTokens.DidNotReceive().ListActiveByUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_enqueues_one_send_per_channel_when_multi_channel()
    {
        StubTemplateRender();
        var captured = new List<NotificationMessage>();
        await _messages.UpsertAsync(Arg.Do<NotificationMessage>(m => captured.Add(m)), Arg.Any<CancellationToken>());

        var sut = BuildSut();
        var request = BuildRequest(new[] { NotificationChannel.Email, NotificationChannel.Sms });

        var results = await sut.DispatchAsync(request);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Success);
        captured.Should().HaveCount(2);
        captured.Select(m => m.Channel).Should().BeEquivalentTo(new[] { NotificationChannel.Email, NotificationChannel.Sms });
        await _deliveryQueue.Received(2).EnqueueChannelSendAsync(Arg.Any<NotificationChannelSendPayload>(), Arg.Any<CancellationToken>());
    }
}
