using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B;

public class CreateDealerAccountHandler : IRequestHandler<CreateDealerAccountCommand, DealerAccountDto>
{
    private readonly IDealerAccountRepository _dealers;
    private readonly IDealerCustomerLinkRepository _links;
    private readonly ICustomerRepository _customers;
    private readonly IB2BAuthorizationService _authz;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public CreateDealerAccountHandler(
        IDealerAccountRepository dealers,
        IDealerCustomerLinkRepository links,
        ICustomerRepository customers,
        IB2BAuthorizationService authz,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _dealers = dealers;
        _links = links;
        _customers = customers;
        _authz = authz;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<DealerAccountDto> Handle(CreateDealerAccountCommand request, CancellationToken cancellationToken)
    {
        _tenant.RequireTenantId();
        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();
        var isTenantAdmin = callerRoles.Contains(B2BAuthorizationRoles.TenantAdmin);

        Customer? primaryCustomer = null;
        if (request.PrimaryCustomerId.HasValue)
        {
            primaryCustomer = await _customers.GetByIdAsync(request.PrimaryCustomerId.Value, cancellationToken)
                ?? throw new CustomerNotFoundException();
            _tenant.EnsureSameTenant(primaryCustomer.TenantId);

            if (!isTenantAdmin
                && !await _authz.IsCustomerOwnerAsync(request.CurrentUserId, primaryCustomer.Id, cancellationToken))
            {
                throw new B2BForbiddenException("Caller cannot create a dealer for this customer.");
            }
        }
        else if (!isTenantAdmin)
        {
            throw new B2BForbiddenException("Only tenant admins may create dealers without a primary customer.");
        }

        var code = request.Code.Trim();
        if (await _dealers.CodeExistsAsync(code, excludeId: null, cancellationToken))
        {
            throw new DuplicateDealerCodeException();
        }

        var createdBy = request.CurrentUserId == default ? (Guid?)null : request.CurrentUserId;
        var dealer = new DealerAccount(
            code,
            request.Name.Trim(),
            createdBy,
            legalName: request.LegalName,
            taxNumber: request.TaxNumber,
            email: request.Email,
            phone: request.Phone,
            address: request.Address,
            notes: request.Notes);

        await _dealers.AddAsync(dealer, cancellationToken);

        if (primaryCustomer is not null)
        {
            var link = new DealerCustomerLink(dealer.Id, primaryCustomer.Id, createdBy);
            await _links.AddAsync(link, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return B2BMappers.ToDto(dealer);
    }
}

public class UpdateDealerAccountHandler : IRequestHandler<UpdateDealerAccountCommand, DealerAccountDto>
{
    private readonly IDealerAccountRepository _dealers;
    private readonly IB2BAuthorizationService _authz;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public UpdateDealerAccountHandler(
        IDealerAccountRepository dealers,
        IB2BAuthorizationService authz,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _dealers = dealers;
        _authz = authz;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<DealerAccountDto> Handle(UpdateDealerAccountCommand request, CancellationToken cancellationToken)
    {
        var dealer = await _dealers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new DealerAccountNotFoundException();
        _tenant.EnsureSameTenant(dealer.TenantId);

        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();
        if (!await _authz.CanManageDealerAsync(request.CurrentUserId, callerRoles, dealer.Id, cancellationToken))
        {
            throw new B2BForbiddenException("Caller cannot update this dealer.");
        }

        dealer.Update(
            name: request.Name.Trim(),
            legalName: request.LegalName,
            taxNumber: request.TaxNumber,
            email: request.Email,
            phone: request.Phone,
            address: request.Address,
            notes: request.Notes);

        _dealers.Update(dealer);
        await _uow.SaveChangesAsync(cancellationToken);
        return B2BMappers.ToDto(dealer);
    }
}

public class ListDealerAccountsHandler : IRequestHandler<ListDealerAccountsQuery, IReadOnlyList<DealerAccountDto>>
{
    private readonly IDealerAccountRepository _dealers;
    private readonly ICustomerUserRepository _customerUsers;
    private readonly ITenantContext _tenant;

    public ListDealerAccountsHandler(
        IDealerAccountRepository dealers,
        ICustomerUserRepository customerUsers,
        ITenantContext tenant)
    {
        _dealers = dealers;
        _customerUsers = customerUsers;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<DealerAccountDto>> Handle(ListDealerAccountsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();
        var isTenantAdmin = callerRoles.Contains(B2BAuthorizationRoles.TenantAdmin);

        IReadOnlyList<DealerAccount> dealers;
        if (request.CustomerId.HasValue)
        {
            dealers = await _dealers.ListByCustomerAsync(request.CustomerId.Value, cancellationToken);
        }
        else if (isTenantAdmin)
        {
            dealers = await _dealers.ListAsync(cancellationToken);
        }
        else
        {
            var owned = await _customerUsers.ListActiveByUserAsync(request.CurrentUserId, tenantId, cancellationToken);
            var ownedCustomerIds = owned
                .Where(m => m.MembershipRole == Domain.Enums.CustomerMembershipRole.CustomerOwner)
                .Select(m => m.CustomerId)
                .Distinct()
                .ToList();
            if (ownedCustomerIds.Count == 0) return Array.Empty<DealerAccountDto>();

            var accumulated = new Dictionary<Guid, DealerAccount>();
            foreach (var customerId in ownedCustomerIds)
            {
                var perCustomer = await _dealers.ListByCustomerAsync(customerId, cancellationToken);
                foreach (var d in perCustomer)
                {
                    accumulated[d.Id] = d;
                }
            }
            dealers = accumulated.Values.OrderBy(d => d.Name).ToList();
        }

        return dealers.Select(B2BMappers.ToDto).ToList();
    }
}
