using CoreAlign.Application.Providers.Commands;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Providers.Queries;

public sealed class GetTenantProviderConfigsHandler
    : IRequestHandler<GetTenantProviderConfigsQuery, IReadOnlyList<TenantProviderConfigDto>>
{
    private readonly ITenantProviderConfigRepository _repository;
    private readonly ITenantContext _tenantContext;

    public GetTenantProviderConfigsHandler(
        ITenantProviderConfigRepository repository,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<TenantProviderConfigDto>> Handle(
        GetTenantProviderConfigsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var configs = await _repository.ListByTenantAsync(tenantId, request.Category, cancellationToken);

        var result = new List<TenantProviderConfigDto>(configs.Count);
        foreach (var config in configs)
        {
            result.Add(UpsertTenantProviderConfigHandler.Map(config));
        }

        return result;
    }
}
