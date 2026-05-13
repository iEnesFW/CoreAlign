using CoreAlign.Application.Accounting.DTOs;
using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Accounting.Commands;

public record CreateAccountingPeriodCommand(int Year, int Month)
    : IRequest<AccountingPeriodDto>, ITransactionalRequest;

public record ClosePeriodCommand(Guid Id, Guid? ClosedByUserId = null, string? Notes = null)
    : IRequest<AccountingPeriodDto>, ITransactionalRequest;

public record ReopenPeriodCommand(Guid Id, Guid? ReopenedByUserId = null)
    : IRequest<AccountingPeriodDto>, ITransactionalRequest;

public record LockPeriodCommand(Guid Id, Guid? LockedByUserId = null)
    : IRequest<AccountingPeriodDto>, ITransactionalRequest;

public record CreateCustomerProductPriceCommand(
    Guid CustomerId,
    Guid ProductId,
    decimal Price,
    string Currency = "TRY",
    decimal? DiscountPercent = null,
    decimal? MinQuantity = null,
    decimal? MaxQuantity = null,
    DateTime? ValidFromUtc = null,
    DateTime? ValidUntilUtc = null,
    string? Notes = null) : IRequest<CustomerProductPriceDto>, ITransactionalRequest;

public record UpdateCustomerProductPriceCommand(
    Guid Id,
    decimal Price,
    string Currency,
    decimal? DiscountPercent,
    decimal? MinQuantity,
    decimal? MaxQuantity,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc,
    string? Notes,
    bool IsActive) : IRequest<CustomerProductPriceDto>, ITransactionalRequest;

public record DeleteCustomerProductPriceCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;
