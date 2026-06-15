using CoreAlign.Application.Common;
using CoreAlign.Application.Documents;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.B2B.DealerPortal;

public record DealerCommissionEntryDto(
    Guid Id,
    Guid OrderId,
    Guid? ShipmentId,
    Guid CustomerId,
    string Currency,
    decimal OrderTotal,
    decimal CommissionPercent,
    decimal CommissionAmount,
    DealerCommissionStatus Status,
    DateTime AccruedAtUtc,
    DateTime? PaidOutAtUtc,
    string? Notes);

public record DealerCommissionSummaryDto(
    decimal YtdAccrued,
    decimal YtdPaid,
    decimal ThisMonthAccrued,
    decimal ThisMonthPaid,
    decimal LifetimeAccrued,
    decimal LifetimePaid,
    string Currency);

public record ListDealerCommissionsQuery(
    DealerCommissionStatus? Status = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<DealerCommissionEntryDto>>;

public record GetDealerCommissionSummaryQuery() : IRequest<DealerCommissionSummaryDto>;

public record DownloadDealerCommissionStatementQuery(
    DateTime FromUtc,
    DateTime ToUtc) : IRequest<DocumentResult>;
