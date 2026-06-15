using CoreAlign.Application.Notifications;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Notifications.Subscribers;
using CoreAlign.Domain.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Notifications;

public class WarrantyActivatedNotificationSubscriberTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();

    [Fact]
    public async Task Handle_dispatches_notification_with_correct_template_key_and_customer_recipient()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var warrantyId = Guid.NewGuid();
        var evt = new WarrantyActivatedEvent(
            TenantId: tenantId,
            WarrantyContractId: warrantyId,
            CustomerId: customerId,
            OrderId: orderId,
            Number: "W-2026-001",
            StartDate: new DateTime(2026, 1, 1),
            EndDate: new DateTime(2027, 1, 1),
            OccurredAtUtc: DateTime.UtcNow);

        NotificationRequest? captured = null;
        _dispatcher
            .DispatchAsync(Arg.Do<NotificationRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NotificationSendResult>());

        var sut = new WarrantyActivatedNotificationSubscriber(
            _dispatcher,
            NullLogger<WarrantyActivatedNotificationSubscriber>.Instance);

        await sut.Handle(evt, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.CustomerId.Should().Be(customerId);
        captured.TemplateKey.Should().Be("Warranty.Activated");
        captured.CategoryKey.Should().Be("Warranty");
        captured.Locale.Should().Be("tr");
        captured.UserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_passes_warranty_metadata_into_payload()
    {
        var evt = new WarrantyActivatedEvent(
            TenantId: Guid.NewGuid(),
            WarrantyContractId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            Number: "W-9",
            StartDate: new DateTime(2026, 3, 15),
            EndDate: new DateTime(2028, 3, 15),
            OccurredAtUtc: DateTime.UtcNow);

        NotificationRequest? captured = null;
        _dispatcher
            .DispatchAsync(Arg.Do<NotificationRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NotificationSendResult>());

        var sut = new WarrantyActivatedNotificationSubscriber(
            _dispatcher,
            NullLogger<WarrantyActivatedNotificationSubscriber>.Instance);

        await sut.Handle(evt, CancellationToken.None);

        captured.Should().NotBeNull();
        var payload = captured!.Payload.Should().BeAssignableTo<IDictionary<string, object?>>().Subject;
        payload["warrantyNumber"].Should().Be("W-9");
        payload["startDate"].Should().Be("2026-03-15");
        payload["endDate"].Should().Be("2028-03-15");
    }

    [Fact]
    public async Task Handle_propagates_cancellation_token_to_dispatcher()
    {
        var evt = new WarrantyActivatedEvent(
            TenantId: Guid.NewGuid(),
            WarrantyContractId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            Number: "W-1",
            StartDate: DateTime.UtcNow.Date,
            EndDate: DateTime.UtcNow.Date.AddYears(2),
            OccurredAtUtc: DateTime.UtcNow);

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        _dispatcher
            .DispatchAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NotificationSendResult>());

        var sut = new WarrantyActivatedNotificationSubscriber(
            _dispatcher,
            NullLogger<WarrantyActivatedNotificationSubscriber>.Instance);

        await sut.Handle(evt, token);

        await _dispatcher.Received(1).DispatchAsync(Arg.Any<NotificationRequest>(), token);
    }
}
