using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Accounting.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Accounting;

public class AccountingPeriodHandlerTests
{
    private readonly IAccountingPeriodRepository _periods = Substitute.For<IAccountingPeriodRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Create_returns_existing_period_without_inserting_when_already_present()
    {
        var existing = new AccountingPeriod(2026, 6);
        _periods.GetByMonthAsync(2026, 6, Arg.Any<CancellationToken>()).Returns(existing);

        var sut = new CreateAccountingPeriodHandler(_periods, _uow);
        var dto = await sut.Handle(new CreateAccountingPeriodCommand(2026, 6), default);

        dto.Year.Should().Be(2026);
        dto.Month.Should().Be(6);
        await _periods.DidNotReceive().AddAsync(Arg.Any<AccountingPeriod>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_inserts_new_period_when_missing()
    {
        _periods.GetByMonthAsync(2026, 7, Arg.Any<CancellationToken>()).Returns((AccountingPeriod?)null);

        var sut = new CreateAccountingPeriodHandler(_periods, _uow);
        await sut.Handle(new CreateAccountingPeriodCommand(2026, 7), default);

        await _periods.Received(1).AddAsync(
            Arg.Is<AccountingPeriod>(p => p.Year == 2026 && p.Month == 7),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_loads_then_closes_then_persists()
    {
        var period = new AccountingPeriod(2026, 6) { Id = Guid.NewGuid() };
        _periods.GetByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);

        var sut = new ClosePeriodHandler(_periods, _uow);
        var dto = await sut.Handle(new ClosePeriodCommand(period.Id, Guid.NewGuid(), "ok"), default);

        dto.Status.Should().Be(AccountingPeriodStatus.Closed);
        _periods.Received(1).Update(period);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_throws_when_period_not_found()
    {
        _periods.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((AccountingPeriod?)null);
        var sut = new ClosePeriodHandler(_periods, _uow);

        Func<Task> act = () => sut.Handle(new ClosePeriodCommand(Guid.NewGuid()), default);
        // A missing/cross-tenant period must surface as a 404 NotFoundException
        // (AccountingPeriodNotFoundException), not a BCL KeyNotFoundException that
        // the middleware maps to 500.
        await act.Should().ThrowAsync<AccountingPeriodNotFoundException>();
    }

    [Fact]
    public async Task Reopen_returns_period_to_open_status()
    {
        var period = new AccountingPeriod(2026, 6) { Id = Guid.NewGuid() };
        period.Close(Guid.NewGuid(), null);
        _periods.GetByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);

        var sut = new ReopenPeriodHandler(_periods, _uow);
        var dto = await sut.Handle(new ReopenPeriodCommand(period.Id, Guid.NewGuid()), default);

        dto.Status.Should().Be(AccountingPeriodStatus.Open);
    }

    [Fact]
    public async Task Lock_marks_period_locked_via_handler()
    {
        var period = new AccountingPeriod(2026, 6) { Id = Guid.NewGuid() };
        _periods.GetByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);

        var sut = new LockPeriodHandler(_periods, _uow);
        var dto = await sut.Handle(new LockPeriodCommand(period.Id, Guid.NewGuid()), default);

        dto.Status.Should().Be(AccountingPeriodStatus.Locked);
    }
}
