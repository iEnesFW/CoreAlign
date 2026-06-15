using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Orders.DTOs;
using MediatR;

namespace CoreAlign.Application.B2B;

/// <summary>
/// Snapshot rendered on the Customer Portal landing page. All counts are
/// scoped to the caller's <c>CustomerId</c> by <see cref="IPortalScopeService"/>;
/// totals on this DTO are intentionally aggregate-only (no per-row PII).
/// </summary>
public record CustomerPortalDashboardDto(
    Guid CustomerId,
    string CustomerName,
    int TotalActiveOrders,
    int TotalOpenInvoices,
    decimal OpenInvoiceTotalAmount,
    string OpenInvoiceCurrency,
    int TotalActiveDealers,
    decimal InvoicedLast30DaysAmount,
    string InvoicedLast30DaysCurrency,
    IReadOnlyList<OrderSummaryDto> RecentOrders,
    IReadOnlyList<InvoiceSummaryDto> RecentInvoices);

public record GetCustomerPortalDashboardQuery() : IRequest<CustomerPortalDashboardDto>;

public record GetCustomerPortalOrdersQuery(
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<OrderSummaryDto>>;

public record GetCustomerPortalOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;

public record GetCustomerPortalInvoicesQuery(
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<InvoiceSummaryDto>>;

public record GetCustomerPortalInvoiceByIdQuery(Guid InvoiceId) : IRequest<InvoiceDto>;

public record GetCustomerPortalDealersQuery() : IRequest<IReadOnlyList<DealerAccountDto>>;

public record GetCustomerPortalPendingApprovalsQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<OrderSummaryDto>>;

public record GetCustomerPortalApprovalByIdQuery(Guid OrderId) : IRequest<OrderDto>;

public record ApproveDealerOrderCommand(Guid OrderId) : IRequest<OrderDto>, ITransactionalRequest;

public record RejectDealerOrderCommand(Guid OrderId, string Reason) : IRequest<OrderDto>, ITransactionalRequest;
