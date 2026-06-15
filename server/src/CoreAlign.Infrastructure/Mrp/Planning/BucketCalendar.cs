using CoreAlign.Domain.Enums;

namespace CoreAlign.Infrastructure.Mrp.Planning;

public sealed class BucketCalendar
{
    private readonly DateTime _anchorUtc;
    private readonly int _daysPerBucket;

    public int Count { get; }
    public IReadOnlyList<DateTime> Starts { get; }

    public BucketCalendar(DateTime asOfUtc, MrpBucketKind kind, int horizonDays)
    {
        _anchorUtc = DateTime.SpecifyKind(asOfUtc.Date, DateTimeKind.Utc);
        _daysPerBucket = kind == MrpBucketKind.Week ? 7 : 1;
        var effectiveHorizon = horizonDays > 0 ? horizonDays : 1;
        Count = (int)Math.Ceiling(effectiveHorizon / (double)_daysPerBucket);
        if (Count < 1)
        {
            Count = 1;
        }

        var starts = new DateTime[Count];
        for (var i = 0; i < Count; i++)
        {
            starts[i] = _anchorUtc.AddDays((long)i * _daysPerBucket);
        }
        Starts = starts;
    }

    public int IndexFor(DateTime dateUtc)
    {
        var day = DateTime.SpecifyKind(dateUtc.Date, DateTimeKind.Utc);
        if (day <= _anchorUtc)
        {
            return 0;
        }
        var offsetDays = (day - _anchorUtc).Days;
        var index = offsetDays / _daysPerBucket;
        return index >= Count ? Count - 1 : index;
    }

    public int OffsetBuckets(int leadTimeDays)
    {
        if (leadTimeDays <= 0)
        {
            return 0;
        }
        return (int)Math.Ceiling(leadTimeDays / (double)_daysPerBucket);
    }

    public DateTime StartOf(int index)
    {
        if (index < 0)
        {
            return Starts[0];
        }
        return index >= Count ? Starts[Count - 1] : Starts[index];
    }
}
