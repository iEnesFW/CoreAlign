using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class DocumentSequence : TenantEntity
{
    public DocumentSequenceType Type { get; private set; }
    public string Prefix { get; private set; } = string.Empty;
    public string? Format { get; private set; }
    public int CurrentYear { get; private set; }
    public long NextNumber { get; private set; } = 1;
    public int PadLength { get; private set; } = 5;

    protected DocumentSequence() { }

    public DocumentSequence(DocumentSequenceType type, string prefix, int year, long startNumber = 1, int padLength = 5, string? format = null)
    {
        Type = type;
        Prefix = prefix;
        CurrentYear = year;
        NextNumber = startNumber;
        PadLength = padLength;
        Format = format;
    }

    public string ConsumeNext(DateTime nowUtc)
    {
        if (nowUtc.Year != CurrentYear)
        {
            CurrentYear = nowUtc.Year;
            NextNumber = 1;
        }
        var number = NextNumber++;
        UpdatedAtUtc = nowUtc;
        return Render(number);
    }

    public string Peek(DateTime nowUtc) => Render(nowUtc.Year != CurrentYear ? 1 : NextNumber);

    private string Render(long number)
    {
        if (!string.IsNullOrEmpty(Format))
        {
            return Format
                .Replace("{P}", Prefix)
                .Replace("{Y}", CurrentYear.ToString("D4"))
                .Replace("{N}", number.ToString($"D{PadLength}"));
        }
        return $"{Prefix}-{CurrentYear:D4}-{number.ToString($"D{PadLength}")}";
    }

    public void ResetForYear(int year, long startNumber = 1)
    {
        CurrentYear = year;
        NextNumber = startNumber;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
