using System.Globalization;
using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Application.Customers.Queries;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Application.Customers.Handlers;

public class GetCustomerAnalyticsQueryHandler : IRequestHandler<GetCustomerAnalyticsQuery, CustomerAnalyticsDto>
{
    private readonly ICustomerRepository _customers;
    private readonly IServiceScopeFactory _scopeFactory;

    public GetCustomerAnalyticsQueryHandler(
        ICustomerRepository customers,
        IServiceScopeFactory scopeFactory)
    {
        _customers = customers;
        _scopeFactory = scopeFactory;
    }

    public async Task<CustomerAnalyticsDto> Handle(GetCustomerAnalyticsQuery request, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(request.Id, ct)
            ?? throw new CustomerNotFoundException();

        var monthsBack = Math.Clamp(request.MonthsBack, 1, 36);
        var firstDayOfStart = new DateTime(
            DateTime.UtcNow.Year,
            DateTime.UtcNow.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc).AddMonths(-(monthsBack - 1));

        // Fan-out: each parallel branch opens its own DI scope (and therefore
        // its own DbContext). This is safe because DbContext is *per-scope*
        // thread-safe — we just can't share one across concurrent awaits, which
        // is what the old sequential code was protecting against. Net effect:
        // 8 sequential round-trips collapse to one wall-clock RTT.
        var customerId = customer.Id;
        var invoiceTotalsTask = RunScopedAsync<ICustomerRepository, (int, decimal, decimal, decimal, string)>(
            (repo, token) => repo.GetInvoiceTotalsAsync(customerId, token), ct);
        var orderTotalsTask = RunScopedAsync<IOrderRepository, (int, decimal, DateTime?, DateTime?)>(
            (repo, token) => repo.GetOrderTotalsExtendedAsync(customerId, token), ct);
        var monthlyTask = RunScopedAsync<IInvoiceRepository, IReadOnlyList<MonthlyInvoiceTotal>>(
            (repo, token) => repo.GetMonthlyRevenueByCustomerAsync(customerId, firstDayOfStart, token), ct);
        var topProductsTask = RunScopedAsync<IInvoiceRepository, IReadOnlyList<TopProductLine>>(
            (repo, token) => repo.GetTopProductsByCustomerAsync(customerId, 5, token), ct);
        var behaviorTask = RunScopedAsync<IInvoiceRepository, PaymentBehavior>(
            (repo, token) => repo.GetPaymentBehaviorByCustomerAsync(customerId, token), ct);
        var invoiceStatusTask = RunScopedAsync<IInvoiceRepository, IReadOnlyList<StatusGroup>>(
            (repo, token) => repo.GetInvoiceStatusBreakdownAsync(customerId, token), ct);
        var orderStatusTask = RunScopedAsync<IOrderRepository, IReadOnlyList<StatusGroup>>(
            (repo, token) => repo.GetOrderStatusBreakdownAsync(customerId, token), ct);
        var paymentSummaryTask = RunScopedAsync<IPaymentRepository, PaymentSummaryAggregate>(
            (repo, token) => repo.GetCustomerPaymentSummaryAsync(customerId, token), ct);

        await Task.WhenAll(
            invoiceTotalsTask, orderTotalsTask, monthlyTask, topProductsTask,
            behaviorTask, invoiceStatusTask, orderStatusTask, paymentSummaryTask);

        var (invoiceCount, invoiced, paid, _, currency) = invoiceTotalsTask.Result;
        var (orderCount, orderTotal, firstOrderAt, lastOrderAt) = orderTotalsTask.Result;
        var monthly = monthlyTask.Result;
        var topProducts = topProductsTask.Result;
        var behavior = behaviorTask.Result;
        var invoiceStatus = invoiceStatusTask.Result;
        var orderStatus = orderStatusTask.Result;
        var paymentSummary = paymentSummaryTask.Result;

        var totalPaidCount = behavior.OnTimePaidCount + behavior.LatePaidCount;
        var onTimeRatio = totalPaidCount > 0
            ? Math.Round((decimal)behavior.OnTimePaidCount / totalPaidCount * 100m, 2)
            : 0m;

        var avgInvoiceValue = invoiceCount > 0 ? Math.Round(invoiced / invoiceCount, 2) : 0m;
        var avgOrderValue = orderCount > 0 ? Math.Round(orderTotal / orderCount, 2) : 0m;

        var lifetimeMonths = (firstOrderAt.HasValue && lastOrderAt.HasValue)
            ? Math.Max(1, ((lastOrderAt.Value.Year - firstOrderAt.Value.Year) * 12)
                + (lastOrderAt.Value.Month - firstOrderAt.Value.Month) + 1)
            : 0;

        var monthlyPoints = BuildMonthlyTimeline(monthly, firstDayOfStart, monthsBack);

        return new CustomerAnalyticsDto
        {
            CustomerId = customer.Id,
            Currency = string.IsNullOrWhiteSpace(currency) ? customer.DefaultCurrency : currency,
            OrderCount = orderCount,
            InvoiceCount = invoiceCount,
            PaymentCount = paymentSummary.Count,
            TotalRevenue = invoiced,
            TotalPaid = paid,
            LifetimeValue = invoiced,
            AvgOrderValue = avgOrderValue,
            AvgInvoiceValue = avgInvoiceValue,
            OnTimePayments = behavior.OnTimePaidCount,
            LatePayments = behavior.LatePaidCount,
            OnTimePaymentRatio = onTimeRatio,
            AvgDaysToPayment = Math.Round(behavior.AvgDaysToPayment, 1),
            FirstOrderAtUtc = firstOrderAt,
            LastOrderAtUtc = lastOrderAt,
            LifetimeMonths = lifetimeMonths,
            MonthlyRevenue = monthlyPoints,
            TopProducts = topProducts.Select(p => new TopProductDto
            {
                ProductId = p.ProductId,
                ProductSku = p.ProductSku,
                ProductName = p.ProductName,
                Quantity = p.Quantity,
                Revenue = p.Revenue,
                InvoiceCount = p.InvoiceCount,
            }).ToList(),
            OrderStatusBreakdown = orderStatus
                .Select(s => new StatusBreakdownDto { Status = s.Status, Count = s.Count, Total = s.Total })
                .OrderByDescending(s => s.Count)
                .ToList(),
            InvoiceStatusBreakdown = invoiceStatus
                .Select(s => new StatusBreakdownDto { Status = s.Status, Count = s.Count, Total = s.Total })
                .OrderByDescending(s => s.Count)
                .ToList(),
        };
    }

    private async Task<TResult> RunScopedAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> body,
        CancellationToken cancellationToken)
        where TService : notnull
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await body(service, cancellationToken);
    }

    private static List<MonthlyRevenuePointDto> BuildMonthlyTimeline(
        IReadOnlyList<MonthlyInvoiceTotal> source,
        DateTime fromUtc,
        int monthsBack)
    {
        var map = source.ToDictionary(s => (s.Year, s.Month), s => s);
        var result = new List<MonthlyRevenuePointDto>(monthsBack);
        for (var i = 0; i < monthsBack; i++)
        {
            var d = fromUtc.AddMonths(i);
            var key = (d.Year, d.Month);
            map.TryGetValue(key, out var found);
            result.Add(new MonthlyRevenuePointDto
            {
                Year = d.Year,
                Month = d.Month,
                Label = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(d.Month) + " " + d.Year.ToString(CultureInfo.InvariantCulture),
                Revenue = found?.Revenue ?? 0m,
                InvoiceCount = found?.InvoiceCount ?? 0,
                Paid = found?.Paid ?? 0m,
            });
        }
        return result;
    }
}
