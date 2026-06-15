using CoreAlign.Application.Tags.DTOs;
using CoreAlign.Application.Tags.Mapping;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Tags;

public sealed class GetCustomerTagsQueryHandler : IRequestHandler<GetCustomerTagsQuery, IReadOnlyList<TagDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerTagLinkRepository _links;
    private readonly ITenantContext _tenantContext;

    public GetCustomerTagsQueryHandler(
        ICustomerRepository customers,
        ICustomerTagLinkRepository links,
        ITenantContext tenantContext)
    {
        _customers = customers;
        _links = links;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<TagDto>> Handle(GetCustomerTagsQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        _tenantContext.EnsureSameTenant(customer.TenantId);

        var map = await _links.GetTagsByCustomersAsync(new[] { customer.Id }, cancellationToken);
        if (!map.TryGetValue(customer.Id, out var tags))
        {
            return Array.Empty<TagDto>();
        }
        return tags.Select(TagMapper.ToDto).ToList();
    }
}
