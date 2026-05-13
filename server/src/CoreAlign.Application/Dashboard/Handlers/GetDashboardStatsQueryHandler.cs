using CoreAlign.Application.Common.Caching;
using CoreAlign.Application.Dashboard.DTOs;
using CoreAlign.Application.Dashboard.Queries;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Orders.Handlers;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Dashboard.Handlers;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private static readonly TimeSpan StatsTtl = TimeSpan.FromSeconds(30);

    private readonly IDashboardStatsRepository _statsRepository;
    private readonly IDashboardCacheService _cache;
    private readonly ITenantContext _tenantContext;

    public GetDashboardStatsQueryHandler(
        IDashboardStatsRepository statsRepository,
        IDashboardCacheService cache,
        ITenantContext tenantContext)
    {
        _statsRepository = statsRepository;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? Guid.Empty;
        var cacheKey = _cache.BuildKey(tenantId, "stats");

        return _cache.GetOrAddAsync(cacheKey, BuildStatsAsync, StatsTtl, cancellationToken);
    }

    private async Task<DashboardStatsDto> BuildStatsAsync(CancellationToken cancellationToken)
    {
        var customerCountTask = _statsRepository.GetCustomerCountAsync(cancellationToken);
        var productCountTask = _statsRepository.GetActiveProductCountAsync(cancellationToken);
        var orderCountsTask = _statsRepository.GetOrderCountByStatusAsync(cancellationToken);
        var totalSalesTask = _statsRepository.GetTotalSalesAsync(cancellationToken);
        var lowStockTask = _statsRepository.GetLowStockProductsAsync(5, cancellationToken);
        var recentOrdersTask = _statsRepository.GetRecentOrdersAsync(5, cancellationToken);
        var salesTrendTask = _statsRepository.GetSalesTrendAsync(30, cancellationToken);
        var invoiceStatsTask = _statsRepository.GetInvoiceStatsAsync(cancellationToken);

        await Task.WhenAll(
            customerCountTask,
            productCountTask,
            orderCountsTask,
            totalSalesTask,
            lowStockTask,
            recentOrdersTask,
            salesTrendTask,
            invoiceStatsTask);

        var orderCounts = await orderCountsTask;
        var lowStock = await lowStockTask;
        var recentOrders = await recentOrdersTask;
        var salesTrend = await salesTrendTask;
        var invoiceStats = await invoiceStatsTask;

        return new DashboardStatsDto
        {
            CustomerCount = await customerCountTask,
            ActiveProductCount = await productCountTask,
            OrderCountByStatus = orderCounts,
            TotalOrderCount = orderCounts.Values.Sum(),
            TotalSales = await totalSalesTask,
            LowStockProducts = lowStock.Select(p => new LowStockProductDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                StockQuantity = p.StockQuantity,
                Unit = p.Unit
            }).ToList(),
            RecentOrders = recentOrders.Select(OrderMapper.ToSummaryDto).ToList(),
            SalesTrend = salesTrend.Select(p => new SalesTrendPointDto { Date = p.Date, Total = p.Total }).ToList(),
            OutstandingReceivables = invoiceStats.Outstanding,
            CollectedThisMonth = invoiceStats.CollectedThisMonth,
            OpenInvoiceCount = invoiceStats.OpenCount
        };
    }
}
