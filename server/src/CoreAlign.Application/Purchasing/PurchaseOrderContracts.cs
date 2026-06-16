using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Purchasing;

public record PurchaseOrderLineInput(
    Guid ProductId,
    decimal Quantity,
    decimal UnitCost,
    decimal TaxRatePercent = 0m,
    Guid? UomId = null,
    string? UomCode = null,
    string? LineNotes = null);

public record PurchaseOrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal Quantity,
    decimal QuantityReceived,
    decimal QuantityBilled,
    decimal QuantityRemainingToReceive,
    decimal UnitCost,
    decimal TaxRatePercent,
    decimal TaxAmount,
    decimal LineSubtotal,
    decimal LineTotal,
    Guid? UomId,
    string? UomCode,
    string? LineNotes);

public record PurchaseOrderDto(
    Guid Id,
    string PoNumber,
    Guid VendorId,
    string VendorName,
    DateTime OrderDate,
    DateTime? ExpectedDate,
    string Currency,
    decimal ExchangeRate,
    Guid? WarehouseId,
    PurchaseOrderStatus Status,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    string? Notes,
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    DateTime CreatedAtUtc);

public record CreatePurchaseOrderCommand(
    Guid VendorId,
    DateTime OrderDate,
    string Currency,
    List<PurchaseOrderLineInput> Lines,
    string? PoNumber = null,
    DateTime? ExpectedDate = null,
    decimal ExchangeRate = 1m,
    Guid? WarehouseId = null,
    string? Notes = null) : IRequest<PurchaseOrderDto>, ITransactionalRequest;

public record UpdatePurchaseOrderCommand(
    Guid Id,
    Guid VendorId,
    DateTime OrderDate,
    string Currency,
    List<PurchaseOrderLineInput> Lines,
    DateTime? ExpectedDate = null,
    decimal ExchangeRate = 1m,
    Guid? WarehouseId = null,
    string? Notes = null) : IRequest<PurchaseOrderDto>, ITransactionalRequest;

public record DeletePurchaseOrderCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;
public record SubmitPurchaseOrderCommand(Guid Id) : IRequest<PurchaseOrderDto>, ITransactionalRequest;
public record ApprovePurchaseOrderCommand(Guid Id, Guid ApprovedByUserId = default) : IRequest<PurchaseOrderDto>, ITransactionalRequest;
public record CancelPurchaseOrderCommand(Guid Id, string? Reason = null) : IRequest<PurchaseOrderDto>, ITransactionalRequest;
public record ClosePurchaseOrderCommand(Guid Id) : IRequest<PurchaseOrderDto>, ITransactionalRequest;

public record ReceiptLineInput(Guid OrderLineId, decimal Quantity);

public record ReceivePurchaseOrderCommand(
    Guid Id,
    List<ReceiptLineInput> Lines,
    string IdempotencyKey,
    Guid? WarehouseId = null,
    string? Notes = null,
    Guid? ReceivedByUserId = null) : IRequest<PurchaseOrderDto>, ITransactionalRequest;

public record GetPurchaseOrderByIdQuery(Guid Id) : IRequest<PurchaseOrderDto?>;
public record SearchPurchaseOrdersQuery(
    Guid? VendorId,
    PurchaseOrderStatus? Status,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<PurchaseOrderDto>>;

public record GoodsReceiptLineDto(
    Guid Id,
    int LineNumber,
    Guid PurchaseOrderLineId,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal QuantityReceived,
    decimal UnitCost,
    decimal LineCost,
    Guid? StockMovementId);

public record GoodsReceiptDto(
    Guid Id,
    string GrnNumber,
    Guid VendorId,
    string VendorName,
    Guid PurchaseOrderId,
    string PoNumber,
    DateTime ReceiptDateUtc,
    Guid WarehouseId,
    GoodsReceiptStatus Status,
    Guid? ReceivedByUserId,
    string? Notes,
    string Currency,
    decimal ExchangeRate,
    decimal TotalCost,
    DateTime? ReversedAtUtc,
    Guid? ReversedByUserId,
    string? ReversalReason,
    IReadOnlyList<GoodsReceiptLineDto> Lines,
    DateTime CreatedAtUtc);

public record ReverseGoodsReceiptCommand(
    Guid GrnId,
    string? Reason = null,
    Guid ReversedByUserId = default) : IRequest<GoodsReceiptDto>, ITransactionalRequest;

public record GetGoodsReceiptByIdQuery(Guid Id) : IRequest<GoodsReceiptDto?>;
public record SearchGoodsReceiptsQuery(
    Guid? PurchaseOrderId,
    Guid? VendorId,
    GoodsReceiptStatus? Status,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<GoodsReceiptDto>>;
