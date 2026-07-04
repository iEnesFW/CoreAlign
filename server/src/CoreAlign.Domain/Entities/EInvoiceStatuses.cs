namespace CoreAlign.Domain.Entities;

public static class EInvoiceStatuses
{
    public const string Queued = "Queued";
    public const string Submitted = "Submitted";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";

    private static readonly string[] Known =
    [
        Queued, Submitted, Accepted, Rejected, Failed, Cancelled,
    ];

    public static bool IsTerminal(string? status) =>
        string.Equals(status, Accepted, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, Rejected, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, Cancelled, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? status)
    {
        var trimmed = status?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return Queued;

        foreach (var known in Known)
        {
            if (string.Equals(trimmed, known, StringComparison.OrdinalIgnoreCase))
            {
                return known;
            }
        }

        return trimmed;
    }
}
