using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Accounting;

public class AccountingPeriodDomainTests
{
    [Fact]
    public void Constructor_assigns_code_start_and_end_in_utc()
    {
        var p = new AccountingPeriod(2026, 3);

        p.Code.Should().Be("2026-03");
        p.StartDate.Should().Be(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        p.EndDate.Should().Be(new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc));
        p.StartDate.Kind.Should().Be(DateTimeKind.Utc);
        p.EndDate.Kind.Should().Be(DateTimeKind.Utc);
        p.Status.Should().Be(AccountingPeriodStatus.Open);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Constructor_rejects_invalid_month(int month)
    {
        var act = () => new AccountingPeriod(2026, month);
        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("month");
    }

    [Fact]
    public void Contains_includes_boundaries()
    {
        var p = new AccountingPeriod(2026, 6);
        p.Contains(p.StartDate).Should().BeTrue();
        p.Contains(p.EndDate).Should().BeTrue();
        p.Contains(p.StartDate.AddSeconds(-1)).Should().BeFalse();
        p.Contains(p.EndDate.AddSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void Close_marks_period_closed_and_records_audit_columns()
    {
        var p = new AccountingPeriod(2026, 6);
        var user = Guid.NewGuid();

        p.Close(user, "year-end cutoff");

        p.Status.Should().Be(AccountingPeriodStatus.Closed);
        p.IsClosed.Should().BeTrue();
        p.ClosedByUserId.Should().Be(user);
        p.ClosedAtUtc.Should().NotBeNull();
        p.Notes.Should().Be("year-end cutoff");
    }

    [Fact]
    public void Close_throws_when_period_is_locked()
    {
        var p = new AccountingPeriod(2026, 6);
        p.Lock(Guid.NewGuid());

        var act = () => p.Close(Guid.NewGuid(), null);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Lock_marks_period_locked_and_preserves_existing_close_audit()
    {
        var p = new AccountingPeriod(2026, 6);
        var closer = Guid.NewGuid();
        p.Close(closer, "closed");
        var closedAt = p.ClosedAtUtc;

        p.Lock(Guid.NewGuid());

        p.Status.Should().Be(AccountingPeriodStatus.Locked);
        p.IsClosed.Should().BeTrue();
        p.ClosedByUserId.Should().Be(closer);
        p.ClosedAtUtc.Should().Be(closedAt);
    }

    [Fact]
    public void Lock_seeds_close_audit_when_locked_directly_from_open()
    {
        var p = new AccountingPeriod(2026, 6);
        var locker = Guid.NewGuid();

        p.Lock(locker);

        p.Status.Should().Be(AccountingPeriodStatus.Locked);
        p.ClosedByUserId.Should().Be(locker);
        p.ClosedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Reopen_returns_closed_period_to_open()
    {
        var p = new AccountingPeriod(2026, 6);
        p.Close(Guid.NewGuid(), null);

        var reopener = Guid.NewGuid();
        p.Reopen(reopener);

        p.Status.Should().Be(AccountingPeriodStatus.Open);
        p.IsClosed.Should().BeFalse();
        p.ReopenedByUserId.Should().Be(reopener);
        p.ReopenedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Reopen_throws_when_period_is_locked()
    {
        var p = new AccountingPeriod(2026, 6);
        p.Lock(Guid.NewGuid());

        var act = () => p.Reopen(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsurePostingAllowed_throws_when_inside_closed_period()
    {
        var p = new AccountingPeriod(2026, 6);
        p.Close(Guid.NewGuid(), null);
        var inside = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var act = () => p.EnsurePostingAllowed(inside);
        act.Should().Throw<PeriodClosedException>();
    }

    [Fact]
    public void EnsurePostingAllowed_throws_when_period_is_locked()
    {
        var p = new AccountingPeriod(2026, 6);
        p.Lock(Guid.NewGuid());
        var inside = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var act = () => p.EnsurePostingAllowed(inside);
        act.Should().Throw<PeriodClosedException>();
    }

    [Fact]
    public void EnsurePostingAllowed_silently_passes_when_date_outside_period()
    {
        var p = new AccountingPeriod(2026, 6);
        p.Close(Guid.NewGuid(), null);
        var outside = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        var act = () => p.EnsurePostingAllowed(outside);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsurePostingAllowed_passes_when_period_is_open()
    {
        var p = new AccountingPeriod(2026, 6);
        var inside = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var act = () => p.EnsurePostingAllowed(inside);
        act.Should().NotThrow();
    }
}
