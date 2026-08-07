using CoreAlign.Application.Billing;
using CoreAlign.Application.Billing.Handlers;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.Billing;

public class CreateSubscriptionOrderHandlerTests
{
    private readonly IModuleRepository _modules = Substitute.For<IModuleRepository>();
    private readonly IModulePricePlanRepository _plans = Substitute.For<IModulePricePlanRepository>();
    private readonly ISubscriptionOrderRepository _orders = Substitute.For<ISubscriptionOrderRepository>();
    private readonly IPaymentAttemptRepository _attempts = Substitute.For<IPaymentAttemptRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly CreateSubscriptionOrderHandler _sut;

    public CreateSubscriptionOrderHandlerTests()
    {
        _tenant.RequireTenantId().Returns(_tenantId);
        _tenant.CurrentTenantId.Returns(_tenantId);

        _gateway.Name.Returns("mock");
        _gateway.CreateIntentAsync(Arg.Any<PaymentIntentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentIntentResult("intent-1", "/redirect", PaymentIntentStatus.Pending, null, "{}"));

        _sequences.ConsumeAsync(
                DocumentSequenceType.SubscriptionOrderNumber,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns("SUB-2026-00001");

        var registry = new PaymentGatewayRegistry(new[] { _gateway });
        var options = Options.Create(new BillingOptions { DefaultGatewayName = "mock" });
        _sut = new CreateSubscriptionOrderHandler(_modules, _plans, _orders, _attempts, _sequences, registry, _tenant, _uow, options);
    }

    [Fact]
    public async Task Snapshots_module_and_plan_into_order_items()
    {
        var moduleId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var module = new Module("Sales", "Sales", null, "Sales", null, 0, true, false);
        typeof(Module).GetProperty(nameof(Module.Id))!.SetValue(module, moduleId);

        var plan = new ModulePricePlan(moduleId, "Yearly", "Yıllık", 365, 999m, "TRY", true, 1);
        typeof(ModulePricePlan).GetProperty(nameof(ModulePricePlan.Id))!.SetValue(plan, planId);

        _modules.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { module });
        _plans.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { plan });

        SubscriptionOrder? captured = null;
        await _orders.AddAsync(Arg.Do<SubscriptionOrder>(o => captured = o), Arg.Any<CancellationToken>());

        var command = new CreateSubscriptionOrderCommand(
            new[] { new OrderItemInput(moduleId, planId) },
            GatewayName: "mock",
            CurrentUserId: Guid.NewGuid());

        var result = await _sut.Handle(command, default);

        result.Should().NotBeNull();
        result.GatewayName.Should().Be("mock");
        result.RedirectUrl.Should().Be("/redirect");
        result.Order.Status.Should().Be(SubscriptionOrderStatus.PendingPayment);
        result.Order.TotalAmount.Should().Be(999m);
        result.Order.OrderNumber.Should().Be("SUB-2026-00001");

        captured.Should().NotBeNull();
        captured!.Items.Should().HaveCount(1);
        var item = captured.Items.First();
        item.ModuleCode.Should().Be("Sales");
        item.ModuleName.Should().Be("Sales");
        item.PlanLabel.Should().Be("Yıllık");
        item.DurationDays.Should().Be(365);
        item.UnitPrice.Should().Be(999m);
        item.Currency.Should().Be("TRY");
        captured.GatewayName.Should().Be("mock");
        captured.GatewayIntentId.Should().Be("intent-1");

        await _attempts.Received(1).AddAsync(
            Arg.Is<PaymentAttempt>(a => a.Status == PaymentAttemptStatus.Initiated && a.IntentId == "intent-1"),
            Arg.Any<CancellationToken>());
        // Two saves by design: the document-sequence row must be committed BEFORE ConsumeAsync
        // queries for it, otherwise the first purchase on a tenant whose sequence was never seeded
        // throws (ConsumeAsync reads the DB, not the change tracker). The second save persists the
        // order. Asserting exactly one save here is what let that defect ship.
        await _uow.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_inactive_module()
    {
        var moduleId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var module = new Module("Sales", "Sales", null, null, null, 0, isActive: false, isCore: false);
        typeof(Module).GetProperty(nameof(Module.Id))!.SetValue(module, moduleId);
        var plan = new ModulePricePlan(moduleId, "Yearly", "Yıllık", 365, 999m, "TRY", true, 1);
        typeof(ModulePricePlan).GetProperty(nameof(ModulePricePlan.Id))!.SetValue(plan, planId);

        _modules.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { module });
        _plans.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { plan });

        var command = new CreateSubscriptionOrderCommand(
            new[] { new OrderItemInput(moduleId, planId) },
            GatewayName: "mock",
            CurrentUserId: Guid.NewGuid());

        var act = async () => await _sut.Handle(command, default);
        await act.Should().ThrowAsync<Domain.Exceptions.ModuleNotFoundException>();
    }

    /// <summary>
    /// A retried checkout must replay the first order: no second order number burned, no second
    /// gateway intent (which is a second chance to charge the buyer), and the caller gets the same
    /// hosted-payment URL back so the flow continues where it left off.
    /// </summary>
    [Fact]
    public async Task Retrying_the_same_operation_id_replays_the_first_order()
    {
        var operationId = Guid.NewGuid();
        var existing = new SubscriptionOrder("SUB-2026-00001", Guid.NewGuid(), "TRY", null, operationId)
        {
            TenantId = _tenantId,
        };
        existing.AttachIntent("mock", "intent-1", "/dashboard/billing/mock-approve?orderId=1");
        _orders.GetByOperationIdAsync(operationId, Arg.Any<CancellationToken>()).Returns(existing);

        var command = new CreateSubscriptionOrderCommand(
            new[] { new OrderItemInput(Guid.NewGuid(), Guid.NewGuid()) },
            GatewayName: "mock",
            CurrentUserId: Guid.NewGuid(),
            OperationId: operationId);

        var result = await _sut.Handle(command, default);

        result.Order.OrderNumber.Should().Be("SUB-2026-00001");
        result.RedirectUrl.Should().Be("/dashboard/billing/mock-approve?orderId=1");
        result.IntentId.Should().Be("intent-1");
        await _sequences.DidNotReceive().ConsumeAsync(
            Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _gateway.DidNotReceive().CreateIntentAsync(Arg.Any<PaymentIntentRequest>(), Arg.Any<CancellationToken>());
        await _orders.DidNotReceive().AddAsync(Arg.Any<SubscriptionOrder>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_checkout_without_an_operation_id_never_looks_for_a_replay()
    {
        var moduleId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var module = new Module("Sales", "Sales", null, null, null, 0, isActive: true, isCore: false);
        typeof(Module).GetProperty(nameof(Module.Id))!.SetValue(module, moduleId);
        var plan = new ModulePricePlan(moduleId, "Yearly", "Yıllık", 365, 999m, "TRY", true, 1);
        typeof(ModulePricePlan).GetProperty(nameof(ModulePricePlan.Id))!.SetValue(plan, planId);
        _modules.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { module });
        _plans.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { plan });

        await _sut.Handle(
            new CreateSubscriptionOrderCommand(
                new[] { new OrderItemInput(moduleId, planId) },
                GatewayName: "mock",
                CurrentUserId: Guid.NewGuid()),
            default);

        await _orders.DidNotReceive().GetByOperationIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
