using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class CustomerTransaction : TenantEntity
{
    public Guid CustomerId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public CustomerTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public Guid? InvoiceId { get; set; }
    public Guid? OrderId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Customer Customer { get; set; } = null!;
    public Invoice? Invoice { get; set; }
    public Order? Order { get; set; }

    protected CustomerTransaction() { }

    public CustomerTransaction(Guid customerId, CustomerTransactionType type, decimal amount, string currency)
    {
        CustomerId = customerId;
        Type = type;
        Amount = amount;
        Currency = currency;
    }
}
