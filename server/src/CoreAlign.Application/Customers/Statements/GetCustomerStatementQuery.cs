using MediatR;

namespace CoreAlign.Application.Customers.Statements;

public sealed record GetCustomerStatementQuery(
    Guid CustomerId,
    DateTime? FromUtc,
    DateTime? ToUtc) : IRequest<CustomerStatementDto>;
