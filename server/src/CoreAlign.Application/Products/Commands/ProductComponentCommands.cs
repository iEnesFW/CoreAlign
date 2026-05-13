using CoreAlign.Application.Common;
using CoreAlign.Application.Products.DTOs;
using MediatR;

namespace CoreAlign.Application.Products.Commands;

public record AddProductComponentCommand(
    Guid ParentProductId,
    Guid ComponentProductId,
    decimal Quantity,
    string? Notes
) : IRequest<ProductComponentDto>, ITransactionalRequest;

public record UpdateProductComponentCommand(
    Guid ParentProductId,
    Guid Id,
    decimal Quantity,
    string? Notes
) : IRequest<ProductComponentDto>, ITransactionalRequest;

public record RemoveProductComponentCommand(
    Guid ParentProductId,
    Guid Id
) : IRequest<bool>, ITransactionalRequest;
