using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.B2B.EventHandlers;

public class DealerCommissionAccrualHandler : INotificationHandler<OrderShippedEvent>
{
    private readonly IOrderRepository _orders;
    private readonly IShipmentRepository _shipments;
    private readonly IDealerAccountRepository _dealers;
    private readonly IDealerCustomerLinkRepository _links;
    private readonly IDealerCommissionLedgerRepository _ledger;
    private readonly ILogger<DealerCommissionAccrualHandler> _logger;

    public DealerCommissionAccrualHandler(
        IOrderRepository orders,
        IShipmentRepository shipments,
        IDealerAccountRepository dealers,
        IDealerCustomerLinkRepository links,
        IDealerCommissionLedgerRepository ledger,
        ILogger<DealerCommissionAccrualHandler> logger)
    {
        _orders = orders;
        _shipments = shipments;
        _dealers = dealers;
        _links = links;
        _ledger = ledger;
        _logger = logger;
    }

    public async Task Handle(OrderShippedEvent notification, CancellationToken cancellationToken)
    {
        var order = await _orders.GetWithLinesAndShipmentsAsync(notification.OrderId, cancellationToken);
        if (order is null) return;
        if (order.OriginDealerAccountId is not Guid dealerAccountId) return;

        var alreadyExists = await _ledger.ExistsForOrderAndShipmentAsync(
            dealerAccountId, notification.OrderId, notification.ShipmentId, cancellationToken);
        if (alreadyExists)
        {
            _logger.LogInformation(
                "Dealer commission entry already exists for dealer {DealerId} order {OrderId} shipment {ShipmentId}; skipping.",
                dealerAccountId, notification.OrderId, notification.ShipmentId);
            return;
        }

        var dealer = await _dealers.GetByIdAsync(dealerAccountId, cancellationToken);
        if (dealer is null) return;

        var link = await _links.GetByDealerAndCustomerAsync(dealerAccountId, order.CustomerId, cancellationToken);
        var effectivePercent = link?.CommissionPercentOverride ?? dealer.CommissionPercent;
        if (effectivePercent <= 0m) return;

        var shipment = await _shipments.GetWithLinesAsync(notification.ShipmentId, cancellationToken);
        if (shipment is null) return;

        var shippedLineNet = 0m;
        foreach (var line in shipment.Lines)
        {
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == line.OrderLineId);
            if (orderLine is null) continue;
            shippedLineNet += Math.Round(orderLine.UnitPrice * line.Quantity, 4);
        }

        if (shippedLineNet <= 0m) return;

        var entry = new DealerCommissionLedgerEntry(
            dealerAccountId: dealerAccountId,
            orderId: notification.OrderId,
            shipmentId: notification.ShipmentId,
            customerId: order.CustomerId,
            currency: order.Currency,
            orderTotal: shippedLineNet,
            commissionPercent: effectivePercent,
            accruedAtUtc: notification.OccurredAtUtc,
            notes: notification.IsPartialShipment ? "Partial shipment" : null)
        {
            TenantId = notification.TenantId,
        };

        await _ledger.AddAsync(entry, cancellationToken);
    }
}
