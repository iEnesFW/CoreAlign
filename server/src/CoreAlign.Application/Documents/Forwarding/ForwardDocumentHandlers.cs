using CoreAlign.Application.B2B;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Documents.Forwarding;

public sealed class ForwardCustomerDocumentHandler : IRequestHandler<ForwardCustomerDocumentCommand, ForwardDocumentResult>
{
    private readonly ITenantContext _tenantContext;
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserRepository _users;
    private readonly IDocumentService _documents;
    private readonly IForwardDocumentService _service;

    public ForwardCustomerDocumentHandler(
        ITenantContext tenantContext,
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IUserRepository users,
        IDocumentService documents,
        IForwardDocumentService service)
    {
        _tenantContext = tenantContext;
        _scope = scope;
        _currentUser = currentUser;
        _users = users;
        _documents = documents;
        _service = service;
    }

    public async Task<ForwardDocumentResult> Handle(ForwardCustomerDocumentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var userId = _currentUser.UserIdOrThrow();
        await _service.EnsureWithinLimitAsync(tenantId, userId, cancellationToken);

        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var pdf = request.DocumentType switch
        {
            ForwardableDocumentType.Invoice => await _documents.RenderInvoicePdfForCustomerAsync(request.DocumentId, customerId, cancellationToken),
            ForwardableDocumentType.Order => await _documents.RenderOrderPdfForCustomerAsync(request.DocumentId, customerId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request)),
        };

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        var senderEmail = user?.Email ?? string.Empty;

        return await _service.ForwardAsync(
            new ForwardDocumentContext(tenantId, request.DocumentType, request.DocumentId, request.RecipientEmail, request.IdempotencyKey, userId, customerId, senderEmail, senderEmail, pdf),
            cancellationToken);
    }
}

public sealed class ForwardDealerDocumentHandler : IRequestHandler<ForwardDealerDocumentCommand, ForwardDocumentResult>
{
    private readonly ITenantContext _tenantContext;
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserRepository _users;
    private readonly IDocumentService _documents;
    private readonly IForwardDocumentService _service;

    public ForwardDealerDocumentHandler(
        ITenantContext tenantContext,
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IUserRepository users,
        IDocumentService documents,
        IForwardDocumentService service)
    {
        _tenantContext = tenantContext;
        _scope = scope;
        _currentUser = currentUser;
        _users = users;
        _documents = documents;
        _service = service;
    }

    public async Task<ForwardDocumentResult> Handle(ForwardDealerDocumentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var userId = _currentUser.UserIdOrThrow();
        await _service.EnsureWithinLimitAsync(tenantId, userId, cancellationToken);

        var dealerAccountId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var allowedCustomerIds = await _scope.GetDealerAllowedCustomerIdsAsync(cancellationToken);
        var pdf = request.DocumentType switch
        {
            ForwardableDocumentType.Invoice => await _documents.RenderInvoicePdfForDealerAsync(request.DocumentId, dealerAccountId, allowedCustomerIds, cancellationToken),
            ForwardableDocumentType.Order => await _documents.RenderOrderPdfForDealerAsync(request.DocumentId, dealerAccountId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request)),
        };

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        var senderEmail = user?.Email ?? string.Empty;

        return await _service.ForwardAsync(
            new ForwardDocumentContext(tenantId, request.DocumentType, request.DocumentId, request.RecipientEmail, request.IdempotencyKey, userId, null, senderEmail, senderEmail, pdf),
            cancellationToken);
    }
}
