using CoreAlign.Application.Notifications;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Application.Notifications.Templates;
using CoreAlign.Application.Providers;
using CoreAlign.Domain.Entities;
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
    private readonly IProviderRegistry<IEmailProvider> _emailRegistry = Substitute.For<IProviderRegistry<IEmailProvider>>();
    private readonly IProviderRegistry<ISmsProvider> _smsRegistry = Substitute.For<IProviderRegistry<ISmsProvider>>();
    private readonly IProviderRegistry<IPushNotificationProvider> _pushRegistry = Substitute.For<IProviderRegistry<IPushNotificationProvider>>();
    private readonly IProviderRegistry<IWhatsAppProvider> _whatsAppRegistry = Substitute.For<IProviderRegistry<IWhatsAppProvider>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly IEmailProvider _emailProvider = Substitute.For<IEmailProvider>();
    private readonly ISmsProvider _smsProvider = Substitute.For<ISmsProvider>();
    private readonly IPushNotificationProvider _pushProvider = Substitute.For<IPushNotificationProvider>();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private NotificationDispatcher BuildSut()
    {
        return new NotificationDispatcher(
            _renderer,
            _messages,
            _preferences,
            _users,
            _customers,
            _deviceTokens,
            _emailRegistry,
            _smsRegistry,
            _pushRegistry,
            _whatsAppRegistry,
            _unitOfWork,
            NullLogger<NotificationDispatcher>.Instance);
    }

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
                ["startDate"] = "2026-01-01",
                ["endDate"] = "2027-01-01"
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
    public async Task DispatchAsync_renders_and_calls_email_provider_for_email_channel()
    {
        StubTemplateRender();
        _emailRegistry.TryResolveForTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(_emailProvider);
        _emailProvider.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(NotificationSendResult.Ok("provider-msg-1"));

        var sut = BuildSut();
        var request = BuildRequest(new[] { NotificationChannel.Email });

        var results = await sut.DispatchAsync(request);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        results[0].ProviderMessageId.Should().Be("provider-msg-1");
        await _emailProvider.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "user@example.com" && m.Subject == "Subject"),
            Arg.Any<CancellationToken>());
        await _messages.Received(1).UpsertAsync(
            Arg.Is<NotificationMessage>(m =>
                m.TenantId == _tenantId &&
                m.Channel == NotificationChannel.Email &&
                m.Status == NotificationStatus.Sent &&
                m.RecipientAddress == "user@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_skips_channel_when_user_opted_out()
    {
        StubTemplateRender();
        _emailRegistry.TryResolveForTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(_emailProvider);
        _emailProvider.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(NotificationSendResult.Ok());

        var disabledPref = new NotificationPreference(_tenantId, _userId, "Warranty", NotificationChannel.Email, false);
        _preferences.GetAsync(_tenantId, _userId, "Warranty", NotificationChannel.Email, Arg.Any<CancellationToken>())
            .Returns(disabledPref);

        var sut = BuildSut();
        var request = BuildRequest(new[] { NotificationChannel.Email });

        var results = await sut.DispatchAsync(request);

        results.Should().BeEmpty();
        await _emailProvider.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await _messages.DidNotReceive().UpsertAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_marks_message_failed_and_increments_retry_when_provider_fails()
    {
        StubTemplateRender();
        _emailRegistry.TryResolveForTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(_emailProvider);
        _emailProvider.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(NotificationSendResult.Fail("smtp timeout"));

        NotificationMessage? captured = null;
        await _messages.UpsertAsync(
            Arg.Do<NotificationMessage>(m => captured = m),
            Arg.Any<CancellationToken>());

        var sut = BuildSut();
        var request = BuildRequest(new[] { NotificationChannel.Email });

        var results = await sut.DispatchAsync(request);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
        results[0].FailureReason.Should().Be("smtp timeout");
        captured.Should().NotBeNull();
        captured!.Status.Should().Be(NotificationStatus.Failed);
        captured.FailureReason.Should().Be("smtp timeout");
        captured.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task DispatchAsync_push_channel_resolves_device_tokens_from_repository_when_no_override()
    {
        StubTemplateRender();
        _pushRegistry.TryResolveForTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(_pushProvider);

        var tokenA = new UserDeviceToken(_tenantId, _userId, "token-a", "ios", "iPhone", "17.0", DateTime.UtcNow);
        var tokenB = new UserDeviceToken(_tenantId, _userId, "token-b", "android", "Pixel", "14", DateTime.UtcNow);
        _deviceTokens.ListActiveByUserAsync(_tenantId, _userId, Arg.Any<CancellationToken>())
            .Returns(new List<UserDeviceToken> { tokenA, tokenB });

        _pushProvider.SendAsync(Arg.Any<PushMessage>(), Arg.Any<CancellationToken>())
            .Returns(NotificationSendResult.Ok("push-ok"));

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
        await _pushProvider.Received(1).SendAsync(
            Arg.Is<PushMessage>(m => m.DeviceToken == "token-a"),
            Arg.Any<CancellationToken>());
        await _pushProvider.Received(1).SendAsync(
            Arg.Is<PushMessage>(m => m.DeviceToken == "token-b"),
            Arg.Any<CancellationToken>());
        await _deviceTokens.Received(1).MarkLastUsedAsync(_tenantId, tokenA.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _deviceTokens.Received(1).MarkLastUsedAsync(_tenantId, tokenB.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_push_channel_returns_NoDeviceTokens_when_repository_empty_and_no_override()
    {
        StubTemplateRender();
        _pushRegistry.TryResolveForTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(_pushProvider);
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
        await _pushProvider.DidNotReceive().SendAsync(Arg.Any<PushMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_push_channel_uses_override_token_when_provided()
    {
        StubTemplateRender();
        _pushRegistry.TryResolveForTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(_pushProvider);
        _pushProvider.SendAsync(Arg.Any<PushMessage>(), Arg.Any<CancellationToken>())
            .Returns(NotificationSendResult.Ok("override-ok"));

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
        await _pushProvider.Received(1).SendAsync(
            Arg.Is<PushMessage>(m => m.DeviceToken == "override-token"),
            Arg.Any<CancellationToken>());
        await _deviceTokens.DidNotReceive().ListActiveByUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_creates_one_NotificationMessage_per_channel_when_multi_channel()
    {
        StubTemplateRender();
        _emailRegistry.TryResolveForTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(_emailProvider);
        _smsRegistry.TryResolveForTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(_smsProvider);
        _emailProvider.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(NotificationSendResult.Ok("email-1"));
        _smsProvider.SendAsync(Arg.Any<SmsMessage>(), Arg.Any<CancellationToken>())
            .Returns(NotificationSendResult.Ok("sms-1"));

        var captured = new List<NotificationMessage>();
        await _messages.UpsertAsync(
            Arg.Do<NotificationMessage>(m => captured.Add(m)),
            Arg.Any<CancellationToken>());

        var sut = BuildSut();
        var request = BuildRequest(new[] { NotificationChannel.Email, NotificationChannel.Sms });

        var results = await sut.DispatchAsync(request);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Success);
        captured.Should().HaveCount(2);
        captured.Select(m => m.Channel).Should().BeEquivalentTo(new[] { NotificationChannel.Email, NotificationChannel.Sms });
        await _emailProvider.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await _smsProvider.Received(1).SendAsync(Arg.Any<SmsMessage>(), Arg.Any<CancellationToken>());
    }
}
