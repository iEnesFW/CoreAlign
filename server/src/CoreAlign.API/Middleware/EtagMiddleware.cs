using System.Security.Cryptography;

namespace CoreAlign.API.Middleware;

public sealed class EtagMiddleware
{
    private const string IfNoneMatchHeader = "If-None-Match";
    private const string ETagHeader = "ETag";
    private const string RangeHeader = "Range";
    private const string ApplicationJsonPrefix = "application/json";
    private const string TextPrefix = "text/";
    private const long MaxBufferableBodyBytes = 1024 * 1024;

    private static readonly string[] SkippedPathSegments =
    {
        "/api/v1/reports",
        "/api/v1/documents",
        "/api/v1/files",
        "/api/v1/quotes",
        "/api/v1/customerportal",
        "/api/v1/dealerportal",
        "/api/v1/taxdeclarations",
    };

    private readonly RequestDelegate _next;

    public EtagMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) || ShouldSkipRequest(context))
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            if (!IsEligibleForEtag(context))
            {
                await CopyBufferToOriginalAsync(buffer, originalBody);
                return;
            }

            buffer.Position = 0;
            var etag = ComputeEtag(buffer);
            context.Response.Headers[ETagHeader] = etag;

            var requestETag = context.Request.Headers[IfNoneMatchHeader].ToString();
            if (!string.IsNullOrEmpty(requestETag) && IsEtagMatch(requestETag, etag))
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                context.Response.ContentLength = 0;
                context.Response.Body = originalBody;
                return;
            }

            buffer.Position = 0;
            context.Response.ContentLength = buffer.Length;
            await CopyBufferToOriginalAsync(buffer, originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool ShouldSkipRequest(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey(RangeHeader))
        {
            return true;
        }

        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        foreach (var segment in SkippedPathSegments)
        {
            if (path.StartsWith(segment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEligibleForEtag(HttpContext context)
    {
        if (context.Response.StatusCode != StatusCodes.Status200OK)
        {
            return false;
        }

        var length = context.Response.Body.Length;
        if (length <= 0 || length > MaxBufferableBodyBytes)
        {
            return false;
        }

        var contentType = context.Response.ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.StartsWith(ApplicationJsonPrefix, StringComparison.OrdinalIgnoreCase)
            || contentType.StartsWith(TextPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeEtag(Stream body)
    {
        body.Position = 0;
        var hash = SHA256.HashData(body);
        var hex = Convert.ToHexString(hash);
        return $"\"{hex}\"";
    }

    private static bool IsEtagMatch(string ifNoneMatch, string etag)
    {
        if (ifNoneMatch == "*")
        {
            return true;
        }

        foreach (var candidate in ifNoneMatch.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = candidate.StartsWith("W/", StringComparison.OrdinalIgnoreCase)
                ? candidate[2..]
                : candidate;
            if (string.Equals(normalized, etag, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task CopyBufferToOriginalAsync(MemoryStream buffer, Stream originalBody)
    {
        buffer.Position = 0;
        await buffer.CopyToAsync(originalBody);
    }
}
