namespace CoreAlign.Infrastructure.Storage;

internal static class StorageKeySanitizer
{
    private static readonly char[] InvalidSegmentChars = { '/', '\\', '\0', ':' };

    public static string SanitizeSegment(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Segment is required.", paramName);
        }

        var trimmed = value.Trim();
        if (trimmed.IndexOfAny(InvalidSegmentChars) >= 0 || trimmed.Contains(".."))
        {
            throw new ArgumentException(
                $"Segment '{value}' contains invalid characters (path separators, NUL, ':' or '..' are forbidden).",
                paramName);
        }

        return trimmed;
    }

    public static string SanitizeRelativePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment is "." or ".." || segment.Contains('\0') || segment.Contains(':'))
            {
                throw new ArgumentException(
                    $"Relative path '{relativePath}' contains forbidden segment '{segment}'.",
                    nameof(relativePath));
            }
        }

        return string.Join('/', segments);
    }
}
