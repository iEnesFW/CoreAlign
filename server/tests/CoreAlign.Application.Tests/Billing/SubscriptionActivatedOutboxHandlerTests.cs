using CoreAlign.Application.Billing;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Billing;

public class SubscriptionActivatedOutboxHandlerTests
{
    private readonly ISubscriptionOrderRepository _orders = Substitute.For<ISubscriptionOrderRepository>();
    private readonly ITenantModuleRepository _tenantModules = Substitute.For<ITenantModuleRepository>();
    private readonly IModuleRepository _modules = Substitute.For<IModuleRepository>();
    private readonly INotificationRepository _notifications = Substitute.For<INotificationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly SubscriptionActivatedOutboxHandler _sut;

    public SubscriptionActivatedOutboxHandlerTests()
    {
        _tenant.CurrentTenantId.Returns(_tenantId);
        _tenant.PushScope(Arg.Any<Guid>()).Returns(new NoopDisposable());
        _users.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>()).Returns(Array.Empty<User>());
        _modules.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<Module>());
        _sut = new SubscriptionActivatedOutboxHandler(_orders, _tenantModules, _modules, _notifications, _users, _tenant, _uow);
    }

    private static SubscriptionOrder BuildPaidOrder(Guid moduleId, Guid orderId)
    {
        var order = new SubscriptionOrder("SUB-2026-00001", Guid.NewGuid(), "TRY");
        typeof(SubscriptionOrder).GetProperty(nameof(SubscriptionOrder.Id))!.SetValue(order, orderId);
        typeof(SubscriptionOrder).GetProperty(nameof(SubscriptionOrder.TenantId))!.SetValue(order, Guid.NewGuid());

        var item = new SubscriptionOrderItem(moduleId, Guid.NewGuid(), "Sales", "Sales", "Aylık", 30, 99m, "TRY");
        order.AddItem(item);
        order.MoveToPendingPayment();
        order.AttachIntent("mock", "intent-1");
        order.MarkPaid("ref");
        return order;
    }

    [Fact]
    public async Task Provisions_new_tenant_module_when_missing()
    {
        var moduleId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = BuildPaidOrder(moduleId, orderId);
        _orders.GetByIdWithDetailsAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _tenantModules.GetByModuleIdAsync(moduleId, Arg.Any<CancellationToken>()).Returns((TenantModule?)null);

        TenantModule? added = null;
        await _tenantModules.AddAsync(Arg.Do<TenantModule>(t => added = t), Arg.Any<CancellationToken>());

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(new SubscriptionActivatedPayload(orderId, _tenantId));
        var result = await _sut.HandleAsync(payloadJson, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        added.Should().NotBeNull();
        added!.ModuleId.Should().Be(moduleId);
        added.Source.Should().Be(TenantModuleSource.Paid);
        added.EndUtc.Should().NotBeNull();
        order.CompletedAtUtc.Should().NotBeNull();
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Extends_existing_tenant_module_on_renewal()
    {
        var moduleId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = BuildPaidOrder(moduleId, orderId);
        _orders.GetByIdWithDetailsAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var existingEnd = DateTime.UtcNow.AddDays(10);
        var existing = new TenantModule(moduleId, DateTime.UtcNow.AddDays(-20), existingEnd, TenantModuleSource.Paid);
        _tenantModules.GetByModuleIdAsync(moduleId, Arg.Any<CancellationToken>()).Returns(existing);

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(new SubscriptionActivatedPayload(orderId, _tenantId));
        var result = await _sut.HandleAsync(payloadJson, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        existing.EndUtc.Should().NotBeNull();
        existing.EndUtc!.Value.Should().BeCloseTo(existingEnd.AddDays(30), TimeSpan.FromMinutes(1));
        await _tenantModules.DidNotReceive().AddAsync(Arg.Any<TenantModule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Is_idempotent_on_already_completed_order()
    {
        var moduleId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = BuildPaidOrder(moduleId, orderId);
        order.MarkCompleted();
        _orders.GetByIdWithDetailsAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(new SubscriptionActivatedPayload(orderId, _tenantId));
        var result = await _sut.HandleAsync(payloadJson, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        result.ResultOrError.Should().Be("AlreadyCompleted");
        await _tenantModules.DidNotReceive().AddAsync(Arg.Any<TenantModule>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
