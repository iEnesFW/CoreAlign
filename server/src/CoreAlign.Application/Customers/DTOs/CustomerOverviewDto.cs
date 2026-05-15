namespace CoreAlign.Application.Customers.DTOs;

public class CustomerOverviewDto
{
    public Guid CustomerId { get; set; }
    public string? GroupName { get; set; }
    public string? SalesRepName { get; set; }
    public string? PriceListName { get; set; }
    public string? PaymentTermsName { get; set; }
    public int? PaymentTermsNetDays { get; set; }
    public CustomerAddressDto? PrimaryBillingAddress { get; set; }
    public CustomerAddressDto? PrimaryShippingAddress { get; set; }
    public CustomerContactDto? PrimaryContact { get; set; }
    public DateTime? LastOrderAtUtc { get; set; }
    public DateTime? LastInvoiceAtUtc { get; set; }
    public DateTime? LastPaymentAtUtc { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal Outstanding { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CreditAvailable { get; set; }
    public decimal CreditUsedPercent { get; set; }
    public bool IsOverCreditLimit { get; set; }
    public List<CustomerActivityItemDto> RecentActivity { get; set; } = new();
}

public class CustomerActivityItemDto
{
    public DateTime OccurredAtUtc { get; set; }
    public string Kind { get; set; } = string.Empty; // Order | Invoice | Payment
    public Guid SourceId { get; set; }
    public string? SourceNumber { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Description { get; set; }
}
