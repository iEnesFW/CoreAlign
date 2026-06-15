using MediatR;

namespace CoreAlign.Application.Customers.Tags;

public sealed record AttachCustomerTagCommand(Guid CustomerId, Guid TagId) : IRequest<Unit>;

public sealed record DetachCustomerTagCommand(Guid CustomerId, Guid TagId) : IRequest<Unit>;
