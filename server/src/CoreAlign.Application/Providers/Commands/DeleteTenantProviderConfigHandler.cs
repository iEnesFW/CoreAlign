using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Providers.Commands;

public sealed class DeleteTenantProviderConfigHandler
    : IRequestHandler<DeleteTenantProviderConfigCommand, Unit>
{
    private readonly ITenantProviderConfigRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantProviderConfigResolver _resolver;

    public DeleteTenantProviderConfigHandler(
        ITenantProviderConfigRepository repository,
        ITenantContext tenantContext,
        ITenantProviderConfigResolver resolver)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _resolver = resolver;
    }

    public async Task<Unit> Handle(DeleteTenantProviderConfigCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();

        var all = await _repository.ListByTenantAsync(tenantId, category: null, cancellationToken);
        var target = all.FirstOrDefault(c => c.Id == request.Id)
            ?? throw new ProviderNotFoundException("TenantProviderConfig", request.Id.ToString());

        _tenantContext.EnsureSameTenant(target.TenantId);

        _repository.Remove(target);
        await _resolver.InvalidateCacheAsync(tenantId, target.Category);

        return Unit.Value;
    }
}
