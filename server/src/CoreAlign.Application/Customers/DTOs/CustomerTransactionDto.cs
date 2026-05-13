namespace CoreAlign.Application.Customers.DTOs;

public class CustomerTransactionDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public Guid? InvoiceId { get; set; }
    public Guid? OrderId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
