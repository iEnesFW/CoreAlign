using CoreAlign.Application.Notifications;
using CoreAlign.Application.Notifications.Delivery;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Application.Notifications.Templates;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Notifications;

public sealed class NotificationConsentGateTests
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

    private NotificationDispatcher Build() => new(
        _renderer, _messages, _preferences, _users, _customers,
        _deviceTokens, _deliveryQueue, _unitOfWork, _consents,
        NullLogger<NotificationDispatcher>.Instance);

    private static NotificationRequest Marketing(Guid userId, string purpose = "marketing") => new(
        TenantId: Guid.NewGuid(),
        UserId: userId,
        CustomerId: null,
        CategoryKey: "Campaign",
        TemplateKey: "campaign.promo",
        Locale: "tr-TR",
        Payload: new { },
        MarketingConsentPurpose: purpose);

    [Fact]
    public async Task Marketing_send_is_blocked_when_no_consent_exists()
    {
        var userId = Guid.NewGuid();
        _consents.GetLatestAsync(userId, "marketing", Arg.Any<CancellationToken>()).Returns((UserConsent?)null);

        var results = await Build().DispatchAsync(Marketing(userId));

        results.Should().ContainSingle();
        results[0].Success.Should().BeFalse();
        results[0].FailureReason.Should().Contain("consent");
        await _renderer.DidNotReceive().RenderAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<NotificationChannel>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
        await _deliveryQueue.DidNotReceive().EnqueueChannelSendAsync(Arg.Any<NotificationChannelSendPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Marketing_send_is_blocked_when_consent_was_withdrawn()
    {
        var userId = Guid.NewGuid();
        var consent = new UserConsent(userId, null, "marketing", "v1", DateTime.UtcNow, null, null);
        consent.Withdraw(DateTime.UtcNow);
        _consents.GetLatestAsync(userId, "marketing", Arg.Any<CancellationToken>()).Returns(consent);

        var results = await Build().DispatchAsync(Marketing(userId));

        results[0].Success.Should().BeFalse();
        await _deliveryQueue.DidNotReceive().EnqueueChannelSendAsync(Arg.Any<NotificationChannelSendPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Marketing_send_proceeds_when_valid_consent_exists()
    {
        var userId = Guid.NewGuid();
        var consent = new UserConsent(userId, null, "marketing", "v1", DateTime.UtcNow, null, null);
        _consents.GetLatestAsync(userId, "marketing", Arg.Any<CancellationToken>()).Returns(consent);
        _renderer.RenderAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<NotificationChannel>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns<RenderedTemplate>(_ => throw new TemplateNotFoundException("campaign.promo", "tr-TR"));

        await Build().DispatchAsync(Marketing(userId));

        await _renderer.Received().RenderAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<NotificationChannel>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transactional_send_does_not_check_consent()
    {
        var request = new NotificationRequest(
            TenantId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            CustomerId: null,
            CategoryKey: "Payment",
            TemplateKey: "payment.succeeded",
            Locale: "tr-TR",
            Payload: new { });
        _renderer.RenderAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<NotificationChannel>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns<RenderedTemplate>(_ => throw new TemplateNotFoundException("payment.succeeded", "tr-TR"));

        await Build().DispatchAsync(request);

        await _consents.DidNotReceive().GetLatestAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
