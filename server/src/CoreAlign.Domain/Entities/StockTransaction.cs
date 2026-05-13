using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class StockTransaction : TenantEntity
{
    public Guid ProductId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public StockTransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal BalanceAfter { get; set; }
    public Guid? OrderId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Product Product { get; set; } = null!;
    public Order? Order { get; set; }

    protected StockTransaction() { }

    public StockTransaction(Guid productId, StockTransactionType type, decimal quantity, decimal balanceAfter)
    {
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        BalanceAfter = balanceAfter;
    }
}
