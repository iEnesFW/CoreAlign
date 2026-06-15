namespace CoreAlign.Application.Common.Audit;

/// <summary>
/// Default redactor — masks values whose field name contains any of the
/// well-known sensitive tokens (passwords, tokens, secrets, tax/identity
/// numbers). Comparison is case-insensitive and substring-based so suffixed
/// variants such as <c>HashedPassword</c> or <c>RefreshToken</c> are caught.
/// </summary>
public sealed class DefaultAuditFieldRedactor : IAuditFieldRedactor
{
    private const string Mask = "***";

    private static readonly string[] SensitiveTokens =
    {
        "password",
        "token",
        "secret",
        "apikey",
        "credential",
        "ssn",
        "vkn",
        "tckn",
    };

    public string? Redact(string fieldName, string? value)
    {
        if (string.IsNullOrEmpty(fieldName) || value is null)
        {
            return value;
        }

        foreach (var token in SensitiveTokens)
        {
            if (fieldName.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return Mask;
            }
        }

        return value;
    }
}
