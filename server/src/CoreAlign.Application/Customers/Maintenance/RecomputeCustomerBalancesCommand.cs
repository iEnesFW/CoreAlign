using MediatR;

namespace CoreAlign.Application.Customers.Maintenance;

public sealed record RecomputeCustomerBalancesCommand(Guid? CustomerId = null, bool DryRun = false)
    : IRequest<RecomputeCustomerBalancesResult>;

public sealed record CustomerBalanceDrift(
    Guid CustomerId,
    string Name,
    decimal StoredBalance,
    decimal LedgerBalance,
    decimal StoredOverdue,
    decimal ComputedOverdue);

public sealed record RecomputeCustomerBalancesResult(
    bool DryRun,
    int Scanned,
    int Drifted,
    int Recomputed,
    decimal LedgerTotal,
    decimal GlControlBalance,
    decimal LedgerVsGlVariance,
    IReadOnlyList<CustomerBalanceDrift> Drifts);
