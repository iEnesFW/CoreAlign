using System.Text.RegularExpressions;
using Sentry;

namespace CoreAlign.API.Observability;

public static partial class SentryPiiScrubber
{
    private static readonly string[] SensitiveHeaders =
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key",
        "X-Auth-Token",
        "Proxy-Authorization",
    };

    private static readonly string[] SensitiveQueryKeys =
    {
        "token",
        "access_token",
        "refresh_token",
        "code",
        "password",
        "api_key",
        "apikey",
    };

    private static readonly string[] SensitivePayloadKeys =
    {
        "password",
        "newpassword",
        "currentpassword",
        "passwordconfirmation",
        "confirmpassword",
        "iban",
        "taxnumber",
        "nationalid",
        "tcno",
        "tckn",
        "ssn",
        "creditcard",
        "cardnumber",
        "cvv",
        "cvc",
        "secretkey",
        "clientsecret",
    };

    private const string Redacted = "[REDACTED]";

    public static SentryEvent? Scrub(SentryEvent evt)
    {
        if (evt.Request is { } req)
        {
            ScrubHeaders(req.Headers);
            req.Cookies = string.IsNullOrEmpty(req.Cookies) ? req.Cookies : Redacted;
            req.QueryString = ScrubQueryString(req.QueryString);
            ScrubData(req.Data);
        }

        if (evt.User is { } user)
        {
            user.IpAddress = null;
            user.Email = null;
            user.Username = null;
        }

        return evt;
    }

    private static void ScrubHeaders(IDictionary<string, string>? headers)
    {
        if (headers is null) return;
        foreach (var key in headers.Keys.ToList())
        {
            if (SensitiveHeaders.Any(h => string.Equals(h, key, StringComparison.OrdinalIgnoreCase)))
            {
                headers[key] = Redacted;
            }
        }
    }

    private static string? ScrubQueryString(string? query)
    {
        if (string.IsNullOrEmpty(query)) return query;
        return QueryParamRegex().Replace(query, match =>
        {
            var key = match.Groups[1].Value;
            return SensitiveQueryKeys.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                ? $"{key}={Redacted}"
                : match.Value;
        });
    }

    private static void ScrubData(object? data)
    {
        if (data is IDictionary<string, object?> dict)
        {
            foreach (var key in dict.Keys.ToList())
            {
                if (IsSensitive(key))
                {
                    dict[key] = Redacted;
                }
                else
                {
                    ScrubData(dict[key]);
                }
            }
        }
    }

    private static bool IsSensitive(string key)
    {
        var normalized = key.Replace("_", string.Empty).Replace("-", string.Empty);
        return SensitivePayloadKeys.Any(s => normalized.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex(@"([A-Za-z0-9_\-]+)=([^&]*)")]
    private static partial Regex QueryParamRegex();
}
