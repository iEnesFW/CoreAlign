using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class AccountingPeriod : TenantEntity
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public AccountingPeriodStatus Status { get; private set; } = AccountingPeriodStatus.Open;
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public DateTime? ReopenedAtUtc { get; private set; }
    public Guid? ReopenedByUserId { get; private set; }
    public string? Notes { get; private set; }

    protected AccountingPeriod() { }

    public AccountingPeriod(int year, int month)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentException("Month must be between 1 and 12.", nameof(month));
        }
        Year = year;
        Month = month;
        Code = $"{year:D4}-{month:D2}";
        StartDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        EndDate = StartDate.AddMonths(1).AddSeconds(-1);
    }

    public bool Contains(DateTime date) => date >= StartDate && date <= EndDate;
    public bool IsClosed => Status == AccountingPeriodStatus.Closed || Status == AccountingPeriodStatus.Locked;

    public void Close(Guid closedByUserId, string? notes)
    {
        if (Status == AccountingPeriodStatus.Locked)
        {
            throw new InvalidOperationException("Locked period cannot be reclosed.");
        }
        Status = AccountingPeriodStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
        ClosedByUserId = closedByUserId;
        Notes = notes;
        UpdatedAtUtc = ClosedAtUtc.Value;
    }

    public void Lock(Guid lockedByUserId)
    {
        Status = AccountingPeriodStatus.Locked;
        ClosedAtUtc ??= DateTime.UtcNow;
        ClosedByUserId ??= lockedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reopen(Guid reopenedByUserId)
    {
        if (Status == AccountingPeriodStatus.Locked)
        {
            throw new InvalidOperationException("Locked period cannot be reopened.");
        }
        Status = AccountingPeriodStatus.Open;
        ReopenedAtUtc = DateTime.UtcNow;
        ReopenedByUserId = reopenedByUserId;
        UpdatedAtUtc = ReopenedAtUtc.Value;
    }

    public void EnsurePostingAllowed(DateTime postingDate)
    {
        if (!Contains(postingDate)) return;
        if (IsClosed)
        {
            throw new PeriodClosedException(postingDate);
        }
    }
}
