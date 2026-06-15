using CoreAlign.Application.Common;
using CoreAlign.Application.Sales.OrderTemplates.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Sales.OrderTemplates.Commands;

public record CreateOrderTemplateCommand(
    string Name,
    Guid CustomerId,
    string Currency,
    OrderFrequency Frequency,
    DateTime? FirstRunAtUtc,
    Guid? PriceListId,
    string? Notes,
    IReadOnlyList<OrderTemplateLineInput> Lines
) : IRequest<OrderTemplateDto>, ITransactionalRequest;

public record UpdateOrderTemplateCommand(
    Guid Id,
    string Name,
    Guid CustomerId,
    string Currency,
    OrderFrequency Frequency,
    DateTime? NextRunAtUtc,
    Guid? PriceListId,
    string? Notes,
    bool IsActive,
    IReadOnlyList<OrderTemplateLineInput> Lines
) : IRequest<OrderTemplateDto>, ITransactionalRequest;

public record DeleteOrderTemplateCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record SetOrderTemplateActiveCommand(Guid Id, bool IsActive) : IRequest<OrderTemplateDto>, ITransactionalRequest;

public record RunOrderTemplateNowCommand(Guid Id) : IRequest<Guid>, ITransactionalRequest;
