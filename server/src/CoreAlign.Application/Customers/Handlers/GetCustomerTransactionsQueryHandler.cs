using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Application.Customers.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Handlers;

public class GetCustomerTransactionsQueryHandler : IRequestHandler<GetCustomerTransactionsQuery, PagedResult<CustomerTransactionDto>>
{
    private readonly ICustomerTransactionRepository _repository;

    public GetCustomerTransactionsQueryHandler(ICustomerTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<CustomerTransactionDto>> Handle(GetCustomerTransactionsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (items, total) = await _repository.GetByCustomerAsync(request.Id, page, pageSize, cancellationToken);
        var dtos = items.Select(t => new CustomerTransactionDto
        {
            Id = t.Id,
            CustomerId = t.CustomerId,
            OccurredAtUtc = t.OccurredAtUtc,
            Type = t.Type.ToString(),
            Amount = t.Amount,
            Currency = t.Currency,
            InvoiceId = t.InvoiceId,
            OrderId = t.OrderId,
            Reference = t.Reference,
            Notes = t.Notes
        }).ToList();

        return new PagedResult<CustomerTransactionDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
