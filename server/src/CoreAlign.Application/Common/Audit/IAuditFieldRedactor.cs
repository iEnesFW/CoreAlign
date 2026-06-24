namespace CoreAlign.Application.Common.Audit;

/// <summary>
/// Hook to strip sensitive material from audit values before they are written
/// to durable storage. Implementations must be deterministic and side-effect
/// free so the same input always redacts to the same output.
/// </summary>
public interface IAuditFieldRedactor
{
    /// <summary>
    /// Returns either the original <paramref name="value"/> or a masked
    /// replacement when <paramref name="fieldName"/> is sensitive.
    /// </summary>
    string? Redact(string fieldName, string? value);

    /// <summary>
    /// Redacts every sensitive property of a serialized JSON object (the audit
    /// before/after payloads) in place, leaving non-sensitive fields untouched.
    /// Invalid or non-object JSON is returned unchanged.
    /// </summary>
    string? RedactJson(string? json);
}
