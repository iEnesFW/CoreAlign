using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IModuleRepository
{
    Task<Module?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Module?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Module>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Module>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Module module, CancellationToken cancellationToken = default);
    void Update(Module module);
}

public interface IModulePricePlanRepository
{
    Task<ModulePricePlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModulePricePlan>> ListByModuleAsync(Guid moduleId, bool activeOnly, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModulePricePlan>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModulePricePlan>> ListAllActiveAsync(CancellationToken cancellationToken = default);
    Task<ModulePricePlan?> GetByModuleAndCodeAsync(Guid moduleId, string code, CancellationToken cancellationToken = default);
    Task AddAsync(ModulePricePlan plan, CancellationToken cancellationToken = default);
    void Update(ModulePricePlan plan);
}

public interface ITenantModuleRepository
{
    Task<TenantModule?> GetByModuleIdAsync(Guid moduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantModule>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TenantModule tenantModule, CancellationToken cancellationToken = default);
    void Update(TenantModule tenantModule);
}

public interface ISubscriptionOrderRepository
{
    Task<SubscriptionOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SubscriptionOrder?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SubscriptionOrder?> GetByGatewayIntentAsync(string gatewayName, string intentId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SubscriptionOrder> Items, int Total)> ListAsync(SubscriptionOrderStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(SubscriptionOrder order, CancellationToken cancellationToken = default);
    void Update(SubscriptionOrder order);
}

public interface IPaymentAttemptRepository
{
    Task AddAsync(PaymentAttempt attempt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentAttempt>> ListByOrderAsync(Guid subscriptionOrderId, CancellationToken cancellationToken = default);
}

public interface IProcessedWebhookEventRepository
{
    Task<bool> ExistsAsync(string gateway, string eventId, string eventType, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedWebhookEvent evt, CancellationToken cancellationToken = default);
}
