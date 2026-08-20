using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.B2B.EventHandlers;

// Commission accrues when goods ship (DealerCommissionAccrualHandler). When the customer sends
// them back the sale did not stick, so the accrual has to come back with them — otherwise the
// dealer is owed commission on revenue that was credited away.
public class DealerCommissionReturnReversalHandler : INotificationHandler<ReturnRequestReceivedEvent>
{
    private readonly IDealerCommissionLedgerRepository _ledger;
    private readonly ILogger<DealerCommissionReturnReversalHandler> _logger;

    public DealerCommissionReturnReversalHandler(
        IDealerCommissionLedgerRepository ledger,
        ILogger<DealerCommissionReturnReversalHandler> logger)
    {
        _ledger = ledger;
        _logger = logger;
    }

    public async Task Handle(ReturnRequestReceivedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.ReturnedLineNet <= 0m) return;

        var entries = await _ledger.ListAccruedByOrderAsync(notification.OrderId, cancellationToken);
        if (entries.Count == 0) return;

        var reason = $"Reversed by return {notification.ReturnNumber}";
        var remaining = notification.ReturnedLineNet;
        foreach (var entry in entries)
        {
            if (remaining <= 0m) break;
            var applied = entry.ReduceBasis(remaining, reason);
            if (applied <= 0m) continue;
            remaining = Math.Round(remaining - applied, 4);
            _ledger.Update(entry);
        }

        if (remaining > 0m)
        {
            // Reachable once commission payouts exist: a Paid entry cannot be edited, so the
            // excess has to be clawed back through the payout process instead of here.
            _logger.LogWarning(
                "Return {ReturnNumber} on order {OrderId} exceeds the accrued dealer commission basis by {Remaining}; the excess was not reversed.",
                notification.ReturnNumber, notification.OrderId, remaining);
        }
    }
}
