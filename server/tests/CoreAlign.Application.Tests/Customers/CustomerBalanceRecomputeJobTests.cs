using CoreAlign.Application.Customers.Maintenance;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Customers;

public class CustomerBalanceRecomputeJobTests
{
    private readonly ICustomerBalanceRecomputeDataSource _data = Substitute.For<ICustomerBalanceRecomputeDataSource>();
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly CustomerBalanceRecomputeJob _sut;

    public CustomerBalanceRecomputeJobTests()
    {
        _sut = new CustomerBalanceRecomputeJob(
            _data, _mediator, _tenantContext, NullLogger<CustomerBalanceRecomputeJob>.Instance);
    }

    private static RecomputeCustomerBalancesResult BuildResult(int recomputed) => new(
        DryRun: false,
        Scanned: 5,
        Drifted: recomputed,
        Recomputed: recomputed,
        LedgerTotal: 0m,
        GlControlBalance: 0m,
        LedgerVsGlVariance: 0m,
        Drifts: []);

    [Fact]
    public async Task Recomputing_runs_once_per_tenant_inside_pushed_scope()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        _data.GetTenantIdsWithCustomersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { tenantA, tenantB });
        _mediator.Send(Arg.Any<RecomputeCustomerBalancesCommand>(), Arg.Any<CancellationToken>())
            .Returns(BuildResult(1));

        await _sut.RunAsync(CancellationToken.None);

        await _mediator.Received(2).Send(Arg.Any<RecomputeCustomerBalancesCommand>(), Arg.Any<CancellationToken>());
        _tenantContext.Received(1).PushScope(tenantA);
        _tenantContext.Received(1).PushScope(tenantB);
    }

    [Fact]
    public async Task Failing_tenant_does_not_stop_remaining_tenants()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        _data.GetTenantIdsWithCustomersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { tenantA, tenantB });
        _mediator.Send(Arg.Any<RecomputeCustomerBalancesCommand>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("boom"),
                _ => BuildResult(2));

        var act = () => _sut.RunAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _mediator.Received(2).Send(Arg.Any<RecomputeCustomerBalancesCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_tenants_results_in_no_recompute_calls()
    {
        _data.GetTenantIdsWithCustomersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        await _sut.RunAsync(CancellationToken.None);

        await _mediator.DidNotReceive().Send(Arg.Any<RecomputeCustomerBalancesCommand>(), Arg.Any<CancellationToken>());
    }
}
