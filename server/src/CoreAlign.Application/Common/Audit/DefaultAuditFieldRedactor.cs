using System.Text.Json.Nodes;

namespace CoreAlign.Application.Common.Audit;

/// <summary>
/// Default redactor — masks values whose field name contains any of the
/// well-known sensitive tokens (passwords, tokens, secrets, tax/identity
/// numbers, bank/card details). Comparison is case-insensitive and
/// substring-based so suffixed variants such as <c>HashedPassword</c>,
/// <c>RefreshToken</c>, or <c>VendorIban</c> are caught.
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
        "nationalid",
        "taxnumber",
        "iban",
        "accountnumber",
        "cardnumber",
        "cvv",
        "cvc",
        "phonenumber",
    };

    private static bool IsSensitive(string fieldName)
    {
        foreach (var token in SensitiveTokens)
        {
            if (fieldName.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public string? Redact(string fieldName, string? value)
    {
        if (string.IsNullOrEmpty(fieldName) || value is null)
        {
            return value;
        }

        return IsSensitive(fieldName) ? Mask : value;
    }

    public string? RedactJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return json;
        }

        try
        {
            if (JsonNode.Parse(json) is not JsonObject root)
            {
                return json;
            }

            RedactObject(root);
            return root.ToJsonString();
        }
        catch (System.Text.Json.JsonException)
        {
            return json;
        }
    }

    private static void RedactObject(JsonObject obj)
    {
        foreach (var key in obj.Select(p => p.Key).ToList())
        {
            switch (obj[key])
            {
                case JsonObject child:
                    RedactObject(child);
                    break;
                case JsonValue when IsSensitive(key):
                    obj[key] = Mask;
                    break;
            }
        }
    }
}
