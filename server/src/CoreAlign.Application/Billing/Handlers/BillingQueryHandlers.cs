using CoreAlign.Application.Billing.DTOs;
using CoreAlign.Application.Billing.Mapping;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Billing.Handlers;

public class ListPaymentGatewaysHandler : IRequestHandler<ListPaymentGatewaysQuery, IReadOnlyList<PaymentGatewayDescriptor>>
{
    private const string MockGatewayName = "mock";

    private readonly IPaymentGatewayRegistry _registry;
    private readonly IOptions<BillingOptions> _options;

    public ListPaymentGatewaysHandler(IPaymentGatewayRegistry registry, IOptions<BillingOptions> options)
    {
        _registry = registry;
        _options = options;
    }

    public Task<IReadOnlyList<PaymentGatewayDescriptor>> Handle(ListPaymentGatewaysQuery request, CancellationToken cancellationToken)
    {
        var defaultName = _options.Value.DefaultGatewayName?.Trim();
        var list = _registry.Names
            .Select(name => new PaymentGatewayDescriptor(
                name,
                BuildDisplayLabel(name),
                RequiresBillingInfo: !string.Equals(name, MockGatewayName, StringComparison.OrdinalIgnoreCase),
                IsDefault: !string.IsNullOrWhiteSpace(defaultName) && string.Equals(name, defaultName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        return Task.FromResult<IReadOnlyList<PaymentGatewayDescriptor>>(list);
    }

    private static string BuildDisplayLabel(string name) => name.ToLowerInvariant() switch
    {
        "mock" => "Mock (Development)",
        "iyzico" => "Iyzico",
        _ => name,
    };
}

public class ListModulesCatalogHandler : IRequestHandler<ListModulesCatalogQuery, IReadOnlyList<ModuleDto>>
{
    private readonly IModuleRepository _modules;
    private readonly IModulePricePlanRepository _plans;

    public ListModulesCatalogHandler(IModuleRepository modules, IModulePricePlanRepository plans)
    {
        _modules = modules;
        _plans = plans;
    }

    public async Task<IReadOnlyList<ModuleDto>> Handle(ListModulesCatalogQuery request, CancellationToken cancellationToken)
    {
        var modules = await _modules.ListAsync(activeOnly: true, cancellationToken);
        if (modules.Count == 0) return Array.Empty<ModuleDto>();

        var plans = await _plans.ListAllActiveAsync(cancellationToken);
        var byModule = plans.GroupBy(p => p.ModuleId).ToDictionary(g => g.Key, g => (IReadOnlyList<Domain.Entities.ModulePricePlan>)g.OrderBy(p => p.SortOrder).ToList());

        return modules
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .Select(m => BillingMapper.ToDto(m, byModule.TryGetValue(m.Id, out var list) ? list : Array.Empty<Domain.Entities.ModulePricePlan>()))
            .ToList();
    }
}

public class ListTenantModulesHandler : IRequestHandler<ListTenantModulesQuery, IReadOnlyList<TenantModuleDto>>
{
    private readonly ITenantModuleRepository _tenantModules;
    private readonly IModuleRepository _modules;

    public ListTenantModulesHandler(ITenantModuleRepository tenantModules, IModuleRepository modules)
    {
        _tenantModules = tenantModules;
        _modules = modules;
    }

    public async Task<IReadOnlyList<TenantModuleDto>> Handle(ListTenantModulesQuery request, CancellationToken cancellationToken)
    {
        var rows = await _tenantModules.ListAsync(cancellationToken);
        if (rows.Count == 0) return Array.Empty<TenantModuleDto>();

        var moduleIds = rows.Select(r => r.ModuleId).Distinct().ToList();
        var modules = (await _modules.ListByIdsAsync(moduleIds, cancellationToken)).ToDictionary(m => m.Id);

        return rows
            .Where(r => modules.ContainsKey(r.ModuleId))
            .Select(r => BillingMapper.ToDto(r, modules[r.ModuleId]))
            .OrderBy(d => d.Name)
            .ToList();
    }
}

public class ListSubscriptionOrdersHandler : IRequestHandler<ListSubscriptionOrdersQuery, PagedResult<SubscriptionOrderDto>>
{
    private readonly ISubscriptionOrderRepository _orders;

    public ListSubscriptionOrdersHandler(ISubscriptionOrderRepository orders) => _orders = orders;

    public async Task<PagedResult<SubscriptionOrderDto>> Handle(ListSubscriptionOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (items, total) = await _orders.ListAsync(request.Status, page, pageSize, cancellationToken);

        return new PagedResult<SubscriptionOrderDto>
        {
            Items = items.Select(BillingMapper.ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetSubscriptionOrderByIdHandler : IRequestHandler<GetSubscriptionOrderByIdQuery, SubscriptionOrderDto>
{
    private readonly ISubscriptionOrderRepository _orders;

    public GetSubscriptionOrderByIdHandler(ISubscriptionOrderRepository orders) => _orders = orders;

    public async Task<SubscriptionOrderDto> Handle(GetSubscriptionOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new SubscriptionOrderNotFoundException();
        return BillingMapper.ToDto(order);
    }
}
