using MediatR;

namespace CoreAlign.Application.CustomerPortal.Credit;

public record CreditSnapshotDto(
    Guid CustomerId,
    string Currency,
    decimal Limit,
    decimal Outstanding,
    decimal Available,
    decimal UsagePercent,
    bool IsSoftLimitReached,
    bool IsHardLimitReached);

public record GetPortalCreditSnapshotQuery() : IRequest<CreditSnapshotDto>;

public record GetDealerCustomerCreditSnapshotQuery(Guid CustomerId) : IRequest<CreditSnapshotDto>;
