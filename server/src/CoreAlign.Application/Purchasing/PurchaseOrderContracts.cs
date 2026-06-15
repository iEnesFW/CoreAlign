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
    Guid? WarehouseId = null,
    string? Notes = null) : IRequest<PurchaseOrderDto>, ITransactionalRequest;

public record GetPurchaseOrderByIdQuery(Guid Id) : IRequest<PurchaseOrderDto?>;
public record SearchPurchaseOrdersQuery(
    Guid? VendorId,
    PurchaseOrderStatus? Status,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<PurchaseOrderDto>>;
