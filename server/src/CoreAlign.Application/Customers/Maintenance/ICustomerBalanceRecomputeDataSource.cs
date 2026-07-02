namespace CoreAlign.Application.Customers.Maintenance;

public interface ICustomerBalanceRecomputeDataSource
{
    Task<IReadOnlyList<Guid>> GetTenantIdsWithCustomersAsync(CancellationToken ct = default);
}
