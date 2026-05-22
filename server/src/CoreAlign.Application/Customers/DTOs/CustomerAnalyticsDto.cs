namespace CoreAlign.Application.Customers.DTOs;

public class CustomerAnalyticsDto
{
    public Guid CustomerId { get; set; }
    public string Currency { get; set; } = "TRY";

    public int OrderCount { get; set; }
    public int InvoiceCount { get; set; }
    public int PaymentCount { get; set; }

    public decimal TotalRevenue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal LifetimeValue { get; set; }
    public decimal AvgOrderValue { get; set; }
    public decimal AvgInvoiceValue { get; set; }

    public int OnTimePayments { get; set; }
    public int LatePayments { get; set; }
    public decimal OnTimePaymentRatio { get; set; }
    public double AvgDaysToPayment { get; set; }

    public DateTime? FirstOrderAtUtc { get; set; }
    public DateTime? LastOrderAtUtc { get; set; }
    public int LifetimeMonths { get; set; }

    public List<MonthlyRevenuePointDto> MonthlyRevenue { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<StatusBreakdownDto> OrderStatusBreakdown { get; set; } = new();
    public List<StatusBreakdownDto> InvoiceStatusBreakdown { get; set; } = new();
}

public class MonthlyRevenuePointDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int InvoiceCount { get; set; }
    public decimal Paid { get; set; }
}

public class TopProductDto
{
    public Guid? ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Revenue { get; set; }
    public int InvoiceCount { get; set; }
}

public class StatusBreakdownDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Total { get; set; }
}
