using CoreAlign.Application.Accounting.DTOs;
using MediatR;

namespace CoreAlign.Application.Accounting.Queries;

public record GetAccountingPeriodByIdQuery(Guid Id) : IRequest<AccountingPeriodDto?>;

public record ListAccountingPeriodsQuery(int? Year = null) : IRequest<IReadOnlyList<AccountingPeriodDto>>;

public record GetCustomerProductPricesQuery(Guid? CustomerId = null, Guid? ProductId = null)
    : IRequest<IReadOnlyList<CustomerProductPriceDto>>;

public record ResolvePriceQuery(
    Guid ProductId,
    Guid CustomerId,
    decimal Quantity = 1m,
    string? Currency = null) : IRequest<ResolvedPriceDto>;
