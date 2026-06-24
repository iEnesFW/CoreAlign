using System.Text.Json;
using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Outbox;

public class OutboxProcessorTests
{
    private readonly IOutboxRepository _outbox = Substitute.For<IOutboxRepository>();
    private readonly IGLPostingService _gl = Substitute.For<IGLPostingService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IOutboxRetryPolicy _retryPolicy = Substitute.For<IOutboxRetryPolicy>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly OutboxProcessor _sut;

    public OutboxProcessorTests()
    {
        _retryPolicy.ComputeNextAttempt(Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(ci => ((DateTime)ci[1]).AddMinutes(1));
        _tenantContext.PushScope(Arg.Any<Guid>()).Returns(Substitute.For<IDisposable>());

        var handlers = new IOutboxMessageHandler[] { new GLPostingOutboxHandler(_gl) };
        _sut = new OutboxProcessor(_outbox, handlers, _uow, _retryPolicy, _tenantContext, NullLogger<OutboxProcessor>.Instance);
    }

    private static OutboxMessage GLMessage(int maxAttempts = OutboxMessage.DefaultMaxAttempts)
    {
        var request = new GLPostingRequest(
            JournalSourceType.SalesInvoice, Guid.NewGuid(), "INV-1", DateTime.UtcNow.Date,
            JournalEntryType.Mahsup, "Satış",
            new[]
            {
                new GLPostingLine(GLPostingKey.AccountsReceivable, 1180m, 0m),
                new GLPostingLine(GLPostingKey.SalesRevenue, 0m, 1180m),
            });
        var json = JsonSerializer.Serialize(request);
        return new OutboxMessage(GLPostingOutbox.MessageType, json, maxAttempts) { Id = Guid.NewGuid() };
    }

    [Fact]
    public async Task Posts_pending_message_and_marks_processed()
    {
        var message = GLMessage();
        _outbox.GetDueAcrossTenantsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(new[] { message });
        _gl.PostAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>()).Returns(GLPostingResult.Posted);

        await _sut.DrainAsync(default);

        await _gl.Received(1).PostAsync(
            Arg.Is<GLPostingRequest>(r => r.SourceType == JournalSourceType.SalesInvoice && r.SourceDocumentNumber == "INV-1"),
            Arg.Any<CancellationToken>());
        message.Status.Should().Be(OutboxStatus.Processed);
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DrainCurrentTenant_uses_tenant_scoped_query_without_pushing_scope()
    {
        var message = GLMessage();
        _outbox.GetDueForCurrentTenantAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(new[] { message });
        _gl.PostAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>()).Returns(GLPostingResult.Posted);

        await _sut.DrainCurrentTenantAsync(default);

        message.Status.Should().Be(OutboxStatus.Processed);
        await _outbox.DidNotReceive().GetDueAcrossTenantsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        _tenantContext.DidNotReceive().PushScope(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Closed_period_result_defers_for_replay()
    {
        var message = GLMessage();
        _outbox.GetDueAcrossTenantsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(new[] { message });
        _gl.PostAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>()).Returns(GLPostingResult.SkippedClosedPeriod);

        await _sut.DrainAsync(default);

        message.Status.Should().Be(OutboxStatus.Deferred);
    }

    [Fact]
    public async Task Transient_failure_schedules_retry()
    {
        var message = GLMessage();
        _outbox.GetDueAcrossTenantsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(new[] { message });
        _gl.PostAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<GLPostingResult>>(_ => throw new InvalidOperationException("boom"));
        _outbox.GetByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);

        await _sut.DrainAsync(default);

        message.Status.Should().Be(OutboxStatus.Pending);
        message.NextAttemptUtc.Should().NotBeNull();
        message.Attempts.Should().Be(1);
        message.LastError.Should().Contain("boom");
        _uow.Received().ClearChangeTracker();
    }

    [Fact]
    public async Task Exhausted_attempts_dead_letters()
    {
        var message = GLMessage(maxAttempts: 1);
        _outbox.GetDueAcrossTenantsAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(new[] { message });
        _gl.PostAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<GLPostingResult>>(_ => throw new InvalidOperationException("boom"));
        _outbox.GetByIdAsync(message.Id, Arg.Any<CancellationToken>()).Returns(message);

        await _sut.DrainAsync(default);

        message.Status.Should().Be(OutboxStatus.DeadLetter);
        message.LastError.Should().Contain("boom");
    }
}
