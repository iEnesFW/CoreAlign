using CoreAlign.Application.Tags.DTOs;
using MediatR;

namespace CoreAlign.Application.Customers.Tags;

public sealed record GetCustomerTagsQuery(Guid CustomerId) : IRequest<IReadOnlyList<TagDto>>;
