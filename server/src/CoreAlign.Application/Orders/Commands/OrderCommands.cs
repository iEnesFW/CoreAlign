using CoreAlign.Application.Common;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Orders.Commands;

public record OrderLineInput(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineDiscountPercent = 0m,
    decimal LineDiscountAmount = 0m,
    decimal TaxRatePercent = 0m,
    bool IsTaxInclusive = false,
    decimal WithholdingRatePercent = 0m,
    Guid? TaxRateId = null,
    Guid? UomId = null,
    string? UomCode = null,
    decimal UomConversionFactor = 1m,
    Guid? WarehouseId = null,
    string? LineNotes = null,
    bool IsManualPriceOverride = false,
    decimal UnitCostSnapshot = 0m);

public record CreateOrderCommand(
    string OrderNumber,
    Guid CustomerId,
    DateTime OrderDate,
    string Currency,
    string? Notes,
    List<OrderLineInput> Lines,
    OrderType Type = OrderType.Standard,
    OrderSource Source = OrderSource.Manual,
    DateTime? RequestedDeliveryDate = null,
    DateTime? PromisedDeliveryDate = null,
    Guid? BillingAddressId = null,
    Guid? ShippingAddressId = null,
    Guid? PaymentTermsId = null,
    Guid? PriceListId = null,
    decimal ExchangeRate = 1m,
    decimal ShippingCost = 0m,
    decimal HeaderDiscountPercent = 0m,
    decimal HeaderDiscountAmount = 0m,
    Guid? SalesRepUserId = null,
    string? Channel = null,
    string? InternalNotes = null,
    string? CustomerNotes = null,
    Guid? OriginOrderId = null
) : IRequest<OrderDto>, ITransactionalRequest;

public record UpdateOrderCommand(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    DateTime OrderDate,
    OrderStatus Status,
    string Currency,
    string? Notes,
    List<OrderLineInput> Lines,
    OrderType Type = OrderType.Standard,
    OrderSource Source = OrderSource.Manual,
    DateTime? RequestedDeliveryDate = null,
    DateTime? PromisedDeliveryDate = null,
    Guid? BillingAddressId = null,
    Guid? ShippingAddressId = null,
    Guid? PaymentTermsId = null,
    Guid? PriceListId = null,
    decimal ExchangeRate = 1m,
    decimal ShippingCost = 0m,
    decimal HeaderDiscountPercent = 0m,
    decimal HeaderDiscountAmount = 0m,
    Guid? SalesRepUserId = null,
    string? Channel = null,
    string? InternalNotes = null,
    string? CustomerNotes = null,
    Guid? OriginOrderId = null
) : IRequest<OrderDto>, ITransactionalRequest;

public record CreateOrderFromPreviousCommand(Guid PreviousOrderId) : IRequest<OrderDto>;

public record DeleteOrderCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record SubmitOrderCommand(Guid Id) : IRequest<OrderDto>, ITransactionalRequest;
public record ApproveOrderCommand(Guid Id, Guid? ApprovedByUserId = null) : IRequest<OrderDto>, ITransactionalRequest;
public record AllocateOrderCommand(Guid Id, Guid? PreferredWarehouseId = null) : IRequest<OrderDto>, ITransactionalRequest;
public record CancelOrderCommand(Guid Id, string? Reason = null) : IRequest<OrderDto>, ITransactionalRequest;
public record DeliverOrderCommand(Guid Id, DateTime? DeliveredAtUtc = null) : IRequest<OrderDto>, ITransactionalRequest;
public record CloseOrderCommand(Guid Id) : IRequest<OrderDto>, ITransactionalRequest;
