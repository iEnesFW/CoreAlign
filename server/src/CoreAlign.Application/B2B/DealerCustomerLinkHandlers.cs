using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B;

public class LinkDealerToCustomerHandler : IRequestHandler<LinkDealerToCustomerCommand, DealerCustomerLinkDto>
{
    private readonly IDealerCustomerLinkRepository _links;
    private readonly IDealerAccountRepository _dealers;
    private readonly ICustomerRepository _customers;
    private readonly IB2BAuthorizationService _authz;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public LinkDealerToCustomerHandler(
        IDealerCustomerLinkRepository links,
        IDealerAccountRepository dealers,
        ICustomerRepository customers,
        IB2BAuthorizationService authz,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _links = links;
        _dealers = dealers;
        _customers = customers;
        _authz = authz;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<DealerCustomerLinkDto> Handle(LinkDealerToCustomerCommand request, CancellationToken cancellationToken)
    {
        var dealer = await _dealers.GetByIdAsync(request.DealerAccountId, cancellationToken)
            ?? throw new DealerAccountNotFoundException();
        _tenant.EnsureSameTenant(dealer.TenantId);

        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        _tenant.EnsureSameTenant(customer.TenantId);

        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();
        if (!await _authz.CanManageCustomerAsync(request.CurrentUserId, callerRoles, customer.Id, cancellationToken))
        {
            throw new B2BForbiddenException("Caller cannot link a dealer to this customer.");
        }

        var assignedBy = request.CurrentUserId == default ? (Guid?)null : request.CurrentUserId;
        var existing = await _links.GetByDealerAndCustomerAsync(dealer.Id, customer.Id, cancellationToken);
        DealerCustomerLink link;
        if (existing is null)
        {
            link = new DealerCustomerLink(dealer.Id, customer.Id, assignedBy, request.Notes);
            await _links.AddAsync(link, cancellationToken);
        }
        else
        {
            if (existing.Status != DealerCustomerLinkStatus.Active)
            {
                existing.Activate();
                _links.Update(existing);
            }
            link = existing;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return B2BMappers.ToDto(link, dealer, customer);
    }
}

public class UnlinkDealerFromCustomerHandler : IRequestHandler<UnlinkDealerFromCustomerCommand, DealerCustomerLinkDto>
{
    private readonly IDealerCustomerLinkRepository _links;
    private readonly IDealerAccountRepository _dealers;
    private readonly ICustomerRepository _customers;
    private readonly IB2BAuthorizationService _authz;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public UnlinkDealerFromCustomerHandler(
        IDealerCustomerLinkRepository links,
        IDealerAccountRepository dealers,
        ICustomerRepository customers,
        IB2BAuthorizationService authz,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _links = links;
        _dealers = dealers;
        _customers = customers;
        _authz = authz;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<DealerCustomerLinkDto> Handle(UnlinkDealerFromCustomerCommand request, CancellationToken cancellationToken)
    {
        var link = await _links.GetByIdAsync(request.LinkId, cancellationToken)
            ?? throw new DealerCustomerLinkNotFoundException();
        _tenant.EnsureSameTenant(link.TenantId);

        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();
        if (!await _authz.CanManageCustomerAsync(request.CurrentUserId, callerRoles, link.CustomerId, cancellationToken))
        {
            throw new B2BForbiddenException("Caller cannot revoke this dealer link.");
        }

        var revokedBy = request.CurrentUserId == default ? (Guid?)null : request.CurrentUserId;
        link.Revoke(revokedBy, request.Reason);
        _links.Update(link);
        await _uow.SaveChangesAsync(cancellationToken);

        var dealer = await _dealers.GetByIdAsync(link.DealerAccountId, cancellationToken)
            ?? throw new DealerAccountNotFoundException();
        var customer = await _customers.GetByIdAsync(link.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        return B2BMappers.ToDto(link, dealer, customer);
    }
}

public class ListDealerCustomerLinksHandler : IRequestHandler<ListDealerCustomerLinksQuery, IReadOnlyList<DealerCustomerLinkDto>>
{
    private readonly IDealerCustomerLinkRepository _links;
    private readonly IDealerAccountRepository _dealers;
    private readonly ICustomerRepository _customers;
    private readonly ICustomerUserRepository _customerUsers;
    private readonly ITenantContext _tenant;

    public ListDealerCustomerLinksHandler(
        IDealerCustomerLinkRepository links,
        IDealerAccountRepository dealers,
        ICustomerRepository customers,
        ICustomerUserRepository customerUsers,
        ITenantContext tenant)
    {
        _links = links;
        _dealers = dealers;
        _customers = customers;
        _customerUsers = customerUsers;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<DealerCustomerLinkDto>> Handle(ListDealerCustomerLinksQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();
        var isTenantAdmin = callerRoles.Contains(B2BAuthorizationRoles.TenantAdmin);

        var rows = await _links.ListByFilterAsync(request.DealerAccountId, request.CustomerId, cancellationToken);
        if (!isTenantAdmin)
        {
            var memberships = await _customerUsers.ListActiveByUserAsync(request.CurrentUserId, tenantId, cancellationToken);
            var ownedCustomerIds = memberships
                .Where(m => m.MembershipRole == CustomerMembershipRole.CustomerOwner)
                .Select(m => m.CustomerId)
                .ToHashSet();
            rows = rows.Where(l => ownedCustomerIds.Contains(l.CustomerId)).ToList();
        }

        if (rows.Count == 0) return Array.Empty<DealerCustomerLinkDto>();

        var result = new List<DealerCustomerLinkDto>(rows.Count);
        foreach (var l in rows)
        {
            var d = await _dealers.GetByIdAsync(l.DealerAccountId, cancellationToken);
            var c = await _customers.GetByIdAsync(l.CustomerId, cancellationToken);
            if (d is null || c is null) continue;
            result.Add(B2BMappers.ToDto(l, d, c));
        }
        return result;
    }
}
