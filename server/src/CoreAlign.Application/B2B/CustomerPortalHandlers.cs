using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Orders.Handlers;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B;

/// <summary>
/// Renders the customer-portal dashboard. The query has no scope parameters
/// because <see cref="IPortalScopeService.GetCurrentCustomerIdAsync"/> resolves
/// the caller's customer id server-side from membership rows.
/// </summary>
public class GetCustomerPortalDashboardHandler : IRequestHandler<GetCustomerPortalDashboardQuery, CustomerPortalDashboardDto>
{
    private const int RecentLimit = 5;
    private const int DashboardListPageSize = 50;

    private static readonly OrderStatus[] OpenOrderStatuses =
    {
        OrderStatus.Submitted,
        OrderStatus.Approved,
        OrderStatus.Confirmed,
        OrderStatus.Allocated,
        OrderStatus.Picking,
        OrderStatus.Packed,
        OrderStatus.PartiallyShipped,
        OrderStatus.Shipped,
    };

    private static readonly InvoiceStatus[] OpenInvoiceStatuses =
    {
        InvoiceStatus.Issued,
        InvoiceStatus.Sent,
        InvoiceStatus.PartiallyPaid,
        InvoiceStatus.Overdue,
    };

    private readonly IPortalScopeService _scope;
    private readonly IOrderRepository _orders;
    private readonly IInvoiceRepository _invoices;
    private readonly IDealerAccountRepository _dealers;
    private readonly ICustomerRepository _customers;

    public GetCustomerPortalDashboardHandler(
        IPortalScopeService scope,
        IOrderRepository orders,
        IInvoiceRepository invoices,
        IDealerAccountRepository dealers,
        ICustomerRepository customers)
    {
        _scope = scope;
        _orders = orders;
        _invoices = invoices;
        _dealers = dealers;
        _customers = customers;
    }

    public async Task<CustomerPortalDashboardDto> Handle(GetCustomerPortalDashboardQuery request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var customer = await _customers.GetByIdAsync(customerId, cancellationToken);
        var customerName = customer?.Name ?? string.Empty;

        var statusBreakdown = await _orders.GetOrderStatusBreakdownAsync(customerId, cancellationToken);
        var openOrderStatusNames = OpenOrderStatuses.Select(s => s.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var totalActiveOrders = statusBreakdown
            .Where(g => openOrderStatusNames.Contains(g.Status))
            .Sum(g => g.Count);

        var invoiceBreakdown = await _invoices.GetInvoiceStatusBreakdownAsync(customerId, cancellationToken);
        var openInvoiceStatusNames = OpenInvoiceStatuses.Select(s => s.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var openInvoiceGroups = invoiceBreakdown
            .Where(g => openInvoiceStatusNames.Contains(g.Status))
            .ToList();
        var totalOpenInvoices = openInvoiceGroups.Sum(g => g.Count);

        var openInvoices = await _invoices.GetOpenForCustomerAsync(customerId, cancellationToken);
        var openInvoiceTotal = openInvoices.Sum(i => i.AmountDue);
        var openInvoiceCurrency = openInvoices
            .GroupBy(i => i.Currency)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? string.Empty;

        var linkedDealers = await _dealers.ListByCustomerAsync(customerId, cancellationToken);
        var totalActiveDealers = linkedDealers.Count(d => d.Status == DealerAccountStatus.Active);

        var (recentOrderRows, _) = await _orders.SearchAsync(null, customerId, 1, RecentLimit, cancellationToken);
        var recentOrders = recentOrderRows.Select(OrderMapper.ToSummaryDto).ToList();

        var (recentInvoiceRows, _) = await _invoices.SearchAsync(null, customerId, 1, RecentLimit, cancellationToken);
        var recentInvoices = recentInvoiceRows.Select(InvoiceMapper.ToSummaryDto).ToList();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var monthlyTotals = await _invoices.GetMonthlyRevenueByCustomerAsync(customerId, thirtyDaysAgo, cancellationToken);
        var invoicedLast30 = monthlyTotals.Sum(m => m.Revenue);
        var invoicedLast30Currency = !string.IsNullOrWhiteSpace(openInvoiceCurrency)
            ? openInvoiceCurrency
            : (customer?.DefaultCurrency ?? string.Empty);

        // DashboardListPageSize is reserved for paged refresh hooks added by
        // later iterations and intentionally unused here — kept as a constant so
        // the magic number does not propagate.
        _ = DashboardListPageSize;

        return new CustomerPortalDashboardDto(
            CustomerId: customerId,
            CustomerName: customerName,
            TotalActiveOrders: totalActiveOrders,
            TotalOpenInvoices: totalOpenInvoices,
            OpenInvoiceTotalAmount: openInvoiceTotal,
            OpenInvoiceCurrency: openInvoiceCurrency,
            TotalActiveDealers: totalActiveDealers,
            InvoicedLast30DaysAmount: invoicedLast30,
            InvoicedLast30DaysCurrency: invoicedLast30Currency,
            RecentOrders: recentOrders,
            RecentInvoices: recentInvoices);
    }
}

public class GetCustomerPortalOrdersHandler : IRequestHandler<GetCustomerPortalOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly IOrderRepository _orders;

    public GetCustomerPortalOrdersHandler(IPortalScopeService scope, IOrderRepository orders)
    {
        _scope = scope;
        _orders = orders;
    }

    public async Task<PagedResult<OrderSummaryDto>> Handle(GetCustomerPortalOrdersQuery request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _orders.SearchAsync(null, customerId, page, pageSize, cancellationToken);

        IEnumerable<OrderSearchRow> filtered = items;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var status))
        {
            filtered = items.Where(i => i.Status == status);
        }

        var dtos = filtered.Select(OrderMapper.ToSummaryDto).ToList();
        return new PagedResult<OrderSummaryDto>
        {
            Items = dtos,
            Total = string.IsNullOrWhiteSpace(request.Status) ? total : dtos.Count,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetCustomerPortalOrderByIdHandler : IRequestHandler<GetCustomerPortalOrderByIdQuery, OrderDto>
{
    private readonly IPortalScopeService _scope;
    private readonly IOrderRepository _orders;

    public GetCustomerPortalOrderByIdHandler(IPortalScopeService scope, IOrderRepository orders)
    {
        _scope = scope;
        _orders = orders;
    }

    public async Task<OrderDto> Handle(GetCustomerPortalOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var order = await _orders.GetWithLinesAsync(request.OrderId, cancellationToken);

        // Same-as-not-found for cross-customer access so the response never leaks
        // the existence of the order to a different customer. The 404 status is
        // produced by the global exception middleware from NotFoundException.
        if (order is null || order.CustomerId != customerId)
        {
            throw new OrderNotFoundException();
        }

        return OrderMapper.ToDto(order);
    }
}

public class GetCustomerPortalInvoicesHandler : IRequestHandler<GetCustomerPortalInvoicesQuery, PagedResult<InvoiceSummaryDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly IInvoiceRepository _invoices;

    public GetCustomerPortalInvoicesHandler(IPortalScopeService scope, IInvoiceRepository invoices)
    {
        _scope = scope;
        _invoices = invoices;
    }

    public async Task<PagedResult<InvoiceSummaryDto>> Handle(GetCustomerPortalInvoicesQuery request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _invoices.SearchAsync(null, customerId, page, pageSize, cancellationToken);

        IEnumerable<InvoiceSearchRow> filtered = items;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<InvoiceStatus>(request.Status, ignoreCase: true, out var status))
        {
            filtered = items.Where(i => i.Status == status);
        }

        var dtos = filtered.Select(InvoiceMapper.ToSummaryDto).ToList();
        return new PagedResult<InvoiceSummaryDto>
        {
            Items = dtos,
            Total = string.IsNullOrWhiteSpace(request.Status) ? total : dtos.Count,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetCustomerPortalInvoiceByIdHandler : IRequestHandler<GetCustomerPortalInvoiceByIdQuery, InvoiceDto>
{
    private readonly IPortalScopeService _scope;
    private readonly IInvoiceRepository _invoices;

    public GetCustomerPortalInvoiceByIdHandler(IPortalScopeService scope, IInvoiceRepository invoices)
    {
        _scope = scope;
        _invoices = invoices;
    }

    public async Task<InvoiceDto> Handle(GetCustomerPortalInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var invoice = await _invoices.GetWithLinesAsync(request.InvoiceId, cancellationToken);

        if (invoice is null || invoice.CustomerId != customerId)
        {
            throw new InvoiceNotFoundException();
        }

        return InvoiceMapper.ToDto(invoice);
    }
}

public class GetCustomerPortalDealersHandler : IRequestHandler<GetCustomerPortalDealersQuery, IReadOnlyList<DealerAccountDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly IDealerAccountRepository _dealers;

    public GetCustomerPortalDealersHandler(IPortalScopeService scope, IDealerAccountRepository dealers)
    {
        _scope = scope;
        _dealers = dealers;
    }

    public async Task<IReadOnlyList<DealerAccountDto>> Handle(GetCustomerPortalDealersQuery request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var dealers = await _dealers.ListByCustomerAsync(customerId, cancellationToken);
        return dealers.Select(B2BMappers.ToDto).ToList();
    }
}
