namespace CoreAlign.Application.Customers.DTOs;

public class CustomerSummaryDto
{
    public Guid CustomerId { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalOrderAmount { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }
    public string Currency { get; set; } = "USD";
}
