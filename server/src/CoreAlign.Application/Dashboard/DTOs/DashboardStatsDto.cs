using CoreAlign.Application.Orders.DTOs;

namespace CoreAlign.Application.Dashboard.DTOs;

public class DashboardStatsDto
{
    public int CustomerCount { get; set; }
    public int ActiveProductCount { get; set; }
    public Dictionary<string, int> OrderCountByStatus { get; set; } = new();
    public int TotalOrderCount { get; set; }
    public decimal TotalSales { get; set; }
    public List<LowStockProductDto> LowStockProducts { get; set; } = new();
    public List<OrderSummaryDto> RecentOrders { get; set; } = new();
    public List<SalesTrendPointDto> SalesTrend { get; set; } = new();
    public decimal OutstandingReceivables { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public int OpenInvoiceCount { get; set; }
}

public class SalesTrendPointDto
{
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
}

public class LowStockProductDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal StockQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
