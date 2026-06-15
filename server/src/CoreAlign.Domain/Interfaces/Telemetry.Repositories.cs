using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface IDashboardStatsRepository
{
    Task<int> GetCustomerCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetActiveProductCountAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetOrderCountByStatusAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalSalesAsync(CancellationToken cancellationToken = default);
    Task<List<Product>> GetLowStockProductsAsync(int take, CancellationToken cancellationToken = default);
    Task<List<Order>> GetRecentOrdersAsync(int take, CancellationToken cancellationToken = default);
    Task<List<(DateTime Date, decimal Total)>> GetSalesTrendAsync(int days, CancellationToken cancellationToken = default);
    Task<(decimal Outstanding, decimal CollectedThisMonth, int OpenCount)> GetInvoiceStatsAsync(CancellationToken cancellationToken = default);
}

public record ActivityLogQueryFilter(
    Guid? UserId = null,
    string? Method = null,
    string? PathContains = null,
    int? StatusCode = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? StatusBucket = null,
    string? EntityType = null,
    Guid? EntityId = null,
    string? Search = null);

public interface IActivityLogRepository
{
    Task AddAsync(ActivityLog log, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ActivityLog> Items, int Total)> GetRecentAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ActivityLog> Items, int Total)> SearchAsync(ActivityLogQueryFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityLog>> StreamAsync(ActivityLogQueryFilter filter, int max, CancellationToken cancellationToken = default);
}
