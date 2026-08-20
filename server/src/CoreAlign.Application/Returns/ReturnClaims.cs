using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Returns;

/// <summary>
/// Quantity per order line that open return requests have already spoken for. Shared so the
/// request-time cap and anything else that needs "how much is still returnable" read the same
/// definition of open.
/// </summary>
public static class ReturnClaims
{
    // Requested and Approved are the only states whose quantity is claimed but not yet inside
    // OrderLine.QuantityReturned: Received (and everything downstream of it) has already advanced
    // that counter, while Rejected and Cancelled release their claim.
    private static readonly ReturnRequestStatus[] OpenStatuses =
    {
        ReturnRequestStatus.Requested,
        ReturnRequestStatus.Approved,
    };

    public static Dictionary<Guid, decimal> ByOrderLine(IEnumerable<ReturnRequest> requests)
    {
        var claimed = new Dictionary<Guid, decimal>();
        foreach (var request in requests.Where(r => OpenStatuses.Contains(r.Status)))
        {
            foreach (var line in request.Lines)
            {
                claimed[line.OrderLineId] = claimed.GetValueOrDefault(line.OrderLineId) + line.QuantityReturned;
            }
        }
        return claimed;
    }
}
