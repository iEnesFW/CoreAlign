using CoreAlign.Application.Common;
using CoreAlign.Application.Products.DTOs;
using MediatR;

namespace CoreAlign.Application.B2B.CustomerPortal;

public record CustomerDirectOrderLineInput(
    Guid ProductId,
    decimal Quantity,
    string? LineNotes = null);

public record CreateCustomerDirectOrderCommand(
    IReadOnlyList<CustomerDirectOrderLineInput> Lines,
    string? Notes = null,
    string? CustomerNotes = null,
    Guid? ShippingAddressId = null,
    Guid? BillingAddressId = null) : IRequest<Guid>, ITransactionalRequest;

public record ListCustomerCatalogProductsQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductDto>>;
