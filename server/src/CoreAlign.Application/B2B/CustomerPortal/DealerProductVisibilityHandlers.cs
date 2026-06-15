using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B.CustomerPortal;

public class GetDealerProductVisibilityHandler
    : IRequestHandler<GetDealerProductVisibilityQuery, DealerProductVisibilityDto>
{
    private readonly IPortalScopeService _scope;
    private readonly IDealerCustomerLinkRepository _links;
    private readonly ICustomerDealerProductVisibilityRepository _visibility;

    public GetDealerProductVisibilityHandler(
        IPortalScopeService scope,
        IDealerCustomerLinkRepository links,
        ICustomerDealerProductVisibilityRepository visibility)
    {
        _scope = scope;
        _links = links;
        _visibility = visibility;
    }

    public async Task<DealerProductVisibilityDto> Handle(
        GetDealerProductVisibilityQuery request,
        CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);

        var link = await _links.GetByIdAsync(request.DealerCustomerLinkId, cancellationToken)
            ?? throw new DealerCustomerLinkNotFoundException();

        if (link.CustomerId != customerId)
        {
            throw new DealerCustomerLinkNotFoundException();
        }

        var hasAny = await _visibility.HasAnyForLinkAsync(link.Id, cancellationToken);
        var ids = hasAny
            ? await _visibility.ListVisibleProductIdsAsync(link.Id, cancellationToken)
            : Array.Empty<Guid>();

        return new DealerProductVisibilityDto
        {
            LinkId = link.Id,
            Mode = hasAny ? DealerProductVisibilityModes.Whitelist : DealerProductVisibilityModes.All,
            VisibleProductIds = ids.ToList(),
        };
    }
}

public class SetDealerProductVisibilityHandler
    : IRequestHandler<SetDealerProductVisibilityCommand, DealerProductVisibilityDto>
{
    private readonly IPortalScopeService _scope;
    private readonly IDealerCustomerLinkRepository _links;
    private readonly ICustomerDealerProductVisibilityRepository _visibility;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;

    public SetDealerProductVisibilityHandler(
        IPortalScopeService scope,
        IDealerCustomerLinkRepository links,
        ICustomerDealerProductVisibilityRepository visibility,
        IProductRepository products,
        IUnitOfWork uow)
    {
        _scope = scope;
        _links = links;
        _visibility = visibility;
        _products = products;
        _uow = uow;
    }

    public async Task<DealerProductVisibilityDto> Handle(
        SetDealerProductVisibilityCommand request,
        CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);

        var link = await _links.GetByIdAsync(request.DealerCustomerLinkId, cancellationToken)
            ?? throw new DealerCustomerLinkNotFoundException();

        if (link.CustomerId != customerId)
        {
            throw new B2BForbiddenException("Caller is not the owner of this dealer-customer link.");
        }

        var existing = await _visibility.ListByLinkAsync(link.Id, cancellationToken);

        if (request.Mode == DealerProductVisibilityModes.All)
        {
            if (existing.Count > 0)
            {
                await _visibility.RemoveRangeAsync(existing, cancellationToken);
                await _uow.SaveChangesAsync(cancellationToken);
            }

            return new DealerProductVisibilityDto
            {
                LinkId = link.Id,
                Mode = DealerProductVisibilityModes.All,
                VisibleProductIds = new List<Guid>(),
            };
        }

        var requestedIds = request.ProductIds.Distinct().ToList();
        if (requestedIds.Count == 0)
        {
            throw new ArgumentException("Whitelist mode requires at least one product id.", nameof(request));
        }

        var foundProducts = await _products.GetByIdsAsync(requestedIds, cancellationToken);
        if (foundProducts.Count != requestedIds.Count)
        {
            throw new ArgumentException("One or more product ids were not found.", nameof(request));
        }

        var existingIds = existing.Select(v => v.ProductId).ToHashSet();
        var requestedSet = requestedIds.ToHashSet();

        var toAdd = requestedSet.Except(existingIds).ToList();
        var toRemove = existing.Where(v => !requestedSet.Contains(v.ProductId)).ToList();

        if (toRemove.Count > 0)
        {
            await _visibility.RemoveRangeAsync(toRemove, cancellationToken);
        }

        foreach (var productId in toAdd)
        {
            await _visibility.AddAsync(new CustomerDealerProductVisibility(link.Id, productId), cancellationToken);
        }

        if (toAdd.Count > 0 || toRemove.Count > 0)
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return new DealerProductVisibilityDto
        {
            LinkId = link.Id,
            Mode = DealerProductVisibilityModes.Whitelist,
            VisibleProductIds = requestedIds,
        };
    }
}
