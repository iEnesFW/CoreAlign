namespace CoreAlign.Application.Reports.DTOs;

public class SalesByPeriodReportDto
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public string Currency { get; set; } = "TRY";
    public List<SalesPeriodPointDto> Points { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalPaid { get; set; }
    public int InvoiceCount { get; set; }
    public int CustomerCount { get; set; }
}

public class SalesPeriodPointDto
{
    public string PeriodKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public DateTime BucketStart { get; set; }
    public decimal Revenue { get; set; }
    public decimal Paid { get; set; }
    public int InvoiceCount { get; set; }
}

public class TopCustomerReportDto
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal TotalRevenue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }
    public int InvoiceCount { get; set; }
    public int OrderCount { get; set; }
    public DateTime? LastOrderAtUtc { get; set; }
}

public class TopProductReportDto
{
    public Guid? ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Revenue { get; set; }
    public int InvoiceCount { get; set; }
}

public class AgingSummaryReportDto
{
    public string Currency { get; set; } = "TRY";
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal DaysOver90 { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int CustomersWithBalance { get; set; }
    public List<CustomerAgingRowDto> ByCustomer { get; set; } = new();
}

public class CustomerAgingRowDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal DaysOver90 { get; set; }
    public decimal TotalOutstanding { get; set; }
}
