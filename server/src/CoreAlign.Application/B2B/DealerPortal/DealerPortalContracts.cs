using CoreAlign.Application.Common;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Products.DTOs;
using MediatR;

namespace CoreAlign.Application.B2B.DealerPortal;

public record DealerPortalDashboardDto(
    Guid DealerAccountId,
    string DealerAccountName,
    int AllowedCustomerCount,
    int PendingApprovalCount,
    int TotalOpenOrders,
    int OrdersCompletedThisMonth,
    IReadOnlyList<OrderSummaryDto> RecentOrders);

public record DealerAllowedCustomerDto(
    Guid CustomerId,
    string? Code,
    string Name,
    string? TaxNumber,
    string Currency,
    Guid? DefaultPriceListId,
    string? DefaultPriceListName);

public record DealerOrderLineInput(
    Guid ProductId,
    decimal Quantity,
    decimal? UnitPrice = null,
    string? LineNotes = null);

public record CreateDealerOrderCommand(
    Guid CustomerId,
    IReadOnlyList<DealerOrderLineInput> Lines,
    string? Notes = null,
    string? Currency = null,
    string? CustomerNotes = null,
    Guid? ShippingAddressId = null,
    Guid? BillingAddressId = null) : IRequest<OrderDto>, ITransactionalRequest;

public record CancelDealerOrderCommand(Guid OrderId, string? Reason = null)
    : IRequest<OrderDto>, ITransactionalRequest;

public record GetDealerPortalDashboardQuery() : IRequest<DealerPortalDashboardDto>;

public record ListDealerAllowedCustomersQuery() : IRequest<IReadOnlyList<DealerAllowedCustomerDto>>;

public record ListDealerOrdersQuery(
    string? Status = null,
    string? ApprovalStatus = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<OrderSummaryDto>>;

public record GetDealerOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;

public record ListDealerCatalogProductsQuery(
    string? Search = null,
    Guid? CustomerId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductDto>>;

public record DealerOrderPricingPreviewLineDto
{
    public Guid ProductId { get; init; }
    public string ProductSku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal ReferenceListPrice { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal TaxRatePercent { get; init; }
    public decimal LineSubtotal { get; init; }
    public decimal LineNetAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal LineTotal { get; init; }
    public decimal? MinOrderQuantity { get; init; }
}

public record DealerOrderPricingPreviewDto
{
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<DealerOrderPricingPreviewLineDto> Lines { get; init; } =
        Array.Empty<DealerOrderPricingPreviewLineDto>();
    public decimal Subtotal { get; init; }
    public decimal TaxTotal { get; init; }
    public decimal Total { get; init; }
}

public record PreviewDealerOrderPricingQuery(
    Guid CustomerId,
    IReadOnlyList<DealerOrderLineInput> Lines,
    string? Currency = null) : IRequest<DealerOrderPricingPreviewDto>;
