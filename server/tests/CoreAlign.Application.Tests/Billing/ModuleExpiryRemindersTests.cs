using CoreAlign.Application.Billing.Expiry;
using CoreAlign.Application.Notifications;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Billing;

public class ModuleExpiryThresholdTests
{
    private static readonly DateTime Now = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(20, null)]
    [InlineData(16, null)]
    [InlineData(15, 15)]
    [InlineData(12, 15)]
    [InlineData(8, 15)]
    [InlineData(7, 7)]
    [InlineData(5, 7)]
    [InlineData(3, 3)]
    [InlineData(2, 3)]
    [InlineData(1, 1)]
    public void The_tightest_threshold_the_grant_qualifies_for_is_used(int daysLeft, int? expected)
    {
        ModuleExpiryThresholds.ResolveThreshold(Now, Now.AddDays(daysLeft)).Should().Be(expected);
    }

    [Fact]
    public void An_already_expired_grant_is_never_reminded_about()
    {
        ModuleExpiryThresholds.ResolveThreshold(Now, Now.AddDays(-1)).Should().BeNull();
        ModuleExpiryThresholds.ResolveThreshold(Now, Now).Should().BeNull();
    }

    /// <summary>
    /// The dispatcher dedups on a hash of the payload. A now-derived value there means a new hash
    /// every day, i.e. the same reminder every single morning until the module lapses.
    /// </summary>
    [Fact]
    public void The_payload_is_stable_across_days_so_the_reminder_is_not_repeated_daily()
    {
        var endUtc = Now.AddDays(10);

        var today = ModuleExpiryThresholds.BuildPayload("Sales", "Satış", endUtc, 15);
        var tomorrow = ModuleExpiryThresholds.BuildPayload("Sales", "Satış", endUtc, 15);

        today.Should().BeEquivalentTo(tomorrow);
        var rendered = string.Join("|", today.Values.Select(v => v?.ToString() ?? string.Empty));
        rendered.Should().NotContain(Now.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Without the threshold in the payload the 3-day warning hashes identically to the 15-day one
    /// and the dispatcher swallows it — the tenant never gets the urgent one.
    /// </summary>
    [Fact]
    public void Each_threshold_produces_a_distinct_payload_so_the_urgent_one_is_not_swallowed()
    {
        var endUtc = Now.AddDays(3);

        var fifteen = ModuleExpiryThresholds.BuildPayload("Sales", "Satış", endUtc, 15);
        var three = ModuleExpiryThresholds.BuildPayload("Sales", "Satış", endUtc, 3);

        fifteen.Should().NotBeEquivalentTo(three);
    }

    [Fact]
    public void The_payload_key_order_is_fixed_because_it_participates_in_the_hash()
    {
        var payload = ModuleExpiryThresholds.BuildPayload("Sales", "Satış", Now.AddDays(5), 7);

        payload.Keys.Should().ContainInOrder("moduleCode", "moduleName", "expiresOn", "thresholdDays");
    }
}

public class ModuleExpiryRemindersJobTests
{
    private readonly IModuleExpiryDataSource _data = Substitute.For<IModuleExpiryDataSource>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly List<NotificationRequest> _sent = new();

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();

    private ModuleExpiryRemindersJob Build(params ExpiringModuleSnapshot[] expiring)
    {
        _data.GetExpiringAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expiring.ToList());
        _data.GetTenantAdminUserIdsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { AdminId });
        _tenant.PushScope(Arg.Any<Guid>()).Returns(Substitute.For<IDisposable>());
        _dispatcher
            .DispatchAsync(Arg.Do<NotificationRequest>(r => _sent.Add(r)), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<NotificationSendResult>>(Array.Empty<NotificationSendResult>()));
        return new ModuleExpiryRemindersJob(_data, _tenant, _dispatcher, NullLogger<ModuleExpiryRemindersJob>.Instance);
    }

    private static ExpiringModuleSnapshot Grant(int daysLeft) =>
        new(TenantId, Guid.NewGuid(), Guid.NewGuid(), "Sales", "Satış", DateTime.UtcNow.AddDays(daysLeft));

    [Fact]
    public async Task A_grant_inside_the_window_warns_the_company_administrator()
    {
        var job = Build(Grant(10));

        await job.RunAsync();

        _sent.Should().ContainSingle();
        _sent[0].UserId.Should().Be(AdminId);
        _sent[0].TemplateKey.Should().Be(ModuleExpiryTemplateKeys.Expiring);
        _sent[0].TenantId.Should().Be(TenantId);
    }

    /// <summary>
    /// Without the tenant scope the dispatcher's dedup read filters to Guid.Empty, always misses,
    /// and the filtered unique index throws into this job's own catch.
    /// </summary>
    [Fact]
    public async Task The_job_pushes_a_tenant_scope_before_dispatching()
    {
        var job = Build(Grant(10));

        await job.RunAsync();

        _tenant.Received(1).PushScope(TenantId);
    }

    [Fact]
    public async Task A_grant_outside_the_window_is_not_announced()
    {
        var job = Build(Grant(40));

        await job.RunAsync();

        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Nothing_is_dispatched_when_the_company_has_no_administrator()
    {
        _data.GetTenantAdminUserIdsAsync(TenantId, Arg.Any<CancellationToken>()).Returns(new List<Guid>());
        var job = Build(Grant(5));
        _data.GetTenantAdminUserIdsAsync(TenantId, Arg.Any<CancellationToken>()).Returns(new List<Guid>());

        await job.RunAsync();

        _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task One_failing_recipient_does_not_stop_the_rest_of_the_run()
    {
        var job = Build(Grant(4), Grant(9));
        _dispatcher
            .DispatchAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("smtp down"),
                _ => Task.FromResult<IReadOnlyList<NotificationSendResult>>(Array.Empty<NotificationSendResult>()));

        var act = async () => await job.RunAsync();

        await act.Should().NotThrowAsync();
    }
}
