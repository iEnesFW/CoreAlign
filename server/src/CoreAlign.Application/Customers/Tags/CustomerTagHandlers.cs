using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Tags;

public sealed class AttachCustomerTagCommandHandler : IRequestHandler<AttachCustomerTagCommand, Unit>
{
    private readonly ICustomerRepository _customers;
    private readonly ITagRepository _tags;
    private readonly ICustomerTagLinkRepository _links;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public AttachCustomerTagCommandHandler(
        ICustomerRepository customers,
        ITagRepository tags,
        ICustomerTagLinkRepository links,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _customers = customers;
        _tags = tags;
        _links = links;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Unit> Handle(AttachCustomerTagCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        _tenantContext.EnsureSameTenant(customer.TenantId);

        var tag = await _tags.GetByIdAsync(request.TagId, cancellationToken)
            ?? throw new TagNotFoundException();
        _tenantContext.EnsureSameTenant(tag.TenantId);

        var added = await _links.AttachAsync(customer.Id, tag.Id, cancellationToken);
        if (added)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return Unit.Value;
    }
}

public sealed class DetachCustomerTagCommandHandler : IRequestHandler<DetachCustomerTagCommand, Unit>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerTagLinkRepository _links;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public DetachCustomerTagCommandHandler(
        ICustomerRepository customers,
        ICustomerTagLinkRepository links,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _customers = customers;
        _links = links;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Unit> Handle(DetachCustomerTagCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        _tenantContext.EnsureSameTenant(customer.TenantId);

        var removed = await _links.DetachAsync(customer.Id, request.TagId, cancellationToken);
        if (removed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return Unit.Value;
    }
}
