using System.Reflection;
using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Mrp;
using CoreAlign.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Mrp;

public class MrpWeeklyJobTests
{
    [Fact]
    public void ComputeDelayUntilNextRun_returns_positive_delay_for_arbitrary_now()
    {
        var now = new DateTime(2026, 6, 4, 12, 0, 0, DateTimeKind.Utc);
        var method = typeof(MrpWeeklyJob).GetMethod("ComputeDelayUntilNextRun", BindingFlags.Static | BindingFlags.NonPublic)!;
        var delay = (TimeSpan)method.Invoke(null, new object[] { now })!;

        delay.Should().BeGreaterThan(TimeSpan.Zero);
        delay.Should().BeLessThanOrEqualTo(TimeSpan.FromDays(7));
    }

    [Fact]
    public async Task RunOnceAsync_invokes_IMrpService_GenerateRequisitionSuggestionsAsync_per_active_tenant()
    {

        var tenantId = Guid.NewGuid();
        var fakeTenant = new RecordingTenantContext { CurrentTenantId = null };
        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseInMemoryDatabase($"mrpjob-{Guid.NewGuid():N}")
            .Options;
        await using (var bootstrap = new CoreAlignDbContext(options, fakeTenant, Substitute.For<IPublisher>()))
        {
            await bootstrap.Database.EnsureCreatedAsync();
            bootstrap.Tenants.Add(new Tenant("Acme", "acme") { Id = tenantId });
            await bootstrap.SaveChangesAsync();
        }

        var mrp = Substitute.For<IMrpService>();
        mrp.GenerateRequisitionSuggestionsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new MrpSuggestionResultDto(0, 0, 0, Array.Empty<Guid>(), DateTime.UtcNow));

        var uow = Substitute.For<IUnitOfWork>();

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton<ITenantContext>(fakeTenant);
        services.AddSingleton<IPublisher>(Substitute.For<IPublisher>());
        services.AddScoped<CoreAlignDbContext>();
        services.AddScoped<IMrpService>(_ => mrp);
        services.AddScoped<IUnitOfWork>(_ => uow);
        var sp = services.BuildServiceProvider();

        var job = new MrpWeeklyJob(sp, NullLogger<MrpWeeklyJob>.Instance);
        var method = typeof(MrpWeeklyJob).GetMethod("RunOnceAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)method.Invoke(job, new object[] { CancellationToken.None })!;

        await mrp.Received(1).GenerateRequisitionSuggestionsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        fakeTenant.PushedTenantIds.Should().Contain(tenantId);
    }

    [Fact]
    public async Task RunOnceAsync_swallows_exception_and_continues_to_next_tenant()
    {

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var fakeTenant = new RecordingTenantContext { CurrentTenantId = null };
        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseInMemoryDatabase($"mrpjob-{Guid.NewGuid():N}")
            .Options;
        await using (var bootstrap = new CoreAlignDbContext(options, fakeTenant, Substitute.For<IPublisher>()))
        {
            await bootstrap.Database.EnsureCreatedAsync();
            bootstrap.Tenants.Add(new Tenant("A", "a") { Id = tenantA });
            bootstrap.Tenants.Add(new Tenant("B", "b") { Id = tenantB });
            await bootstrap.SaveChangesAsync();
        }

        var mrp = Substitute.For<IMrpService>();
        mrp.GenerateRequisitionSuggestionsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns<MrpSuggestionResultDto>(_ => throw new InvalidOperationException("boom"));

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton<ITenantContext>(fakeTenant);
        services.AddSingleton<IPublisher>(Substitute.For<IPublisher>());
        services.AddScoped<CoreAlignDbContext>();
        services.AddScoped<IMrpService>(_ => mrp);
        services.AddScoped<IUnitOfWork>(_ => Substitute.For<IUnitOfWork>());
        var sp = services.BuildServiceProvider();

        var job = new MrpWeeklyJob(sp, NullLogger<MrpWeeklyJob>.Instance);
        var method = typeof(MrpWeeklyJob).GetMethod("RunOnceAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Func<Task> act = () => (Task)method.Invoke(job, new object[] { CancellationToken.None })!;
        await act.Should().NotThrowAsync();

        await mrp.Received(2).GenerateRequisitionSuggestionsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    private sealed class RecordingTenantContext : ITenantContext
    {
        public Guid? CurrentTenantId { get; set; }
        public bool HasTenant => CurrentTenantId is not null;
        public List<Guid> PushedTenantIds { get; } = new();

        public Guid RequireTenantId() => CurrentTenantId ?? throw new InvalidOperationException("tenant not set");
        public void EnsureSameTenant(Guid resourceTenantId) { }
        public IDisposable PushScope(Guid tenantId)
        {
            PushedTenantIds.Add(tenantId);
            var previous = CurrentTenantId;
            CurrentTenantId = tenantId;
            return new Scope(this, previous);
        }

        private sealed class Scope : IDisposable
        {
            private readonly RecordingTenantContext _ctx;
            private readonly Guid? _previous;
            public Scope(RecordingTenantContext ctx, Guid? previous) { _ctx = ctx; _previous = previous; }
            public void Dispose() => _ctx.CurrentTenantId = _previous;
        }
    }
}
