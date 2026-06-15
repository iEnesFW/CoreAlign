using System.Net;
using System.Text;
using CoreAlign.API.Middleware;
using Microsoft.AspNetCore.Http;

namespace CoreAlign.Application.Tests.Api;

public class EtagMiddlewareTests
{
    [Fact]
    public async Task GetJsonResponse_sets_etag_header()
    {
        var context = BuildContext();
        var middleware = new EtagMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"value\":1}");
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers.ETag.ToString().Should().NotBeEmpty();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task PostRequest_skips_etag()
    {
        var context = BuildContext(method: HttpMethods.Post);
        var middleware = new EtagMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"value\":1}");
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers.ETag.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task IfNoneMatch_matches_returns_304_and_empty_body()
    {
        var (firstCtx, firstBody) = await ExecuteAsync();
        var etag = firstCtx.Response.Headers.ETag.ToString();
        etag.Should().NotBeEmpty();

        var (secondCtx, secondBody) = await ExecuteAsync(ifNoneMatch: etag);

        secondCtx.Response.StatusCode.Should().Be(StatusCodes.Status304NotModified);
        secondBody.Length.Should().Be(0);
    }

    [Fact]
    public async Task IfNoneMatch_with_weak_prefix_also_matches()
    {
        var (firstCtx, _) = await ExecuteAsync();
        var etag = firstCtx.Response.Headers.ETag.ToString();
        var weakTag = "W/" + etag;

        var (secondCtx, _) = await ExecuteAsync(ifNoneMatch: weakTag);

        secondCtx.Response.StatusCode.Should().Be(StatusCodes.Status304NotModified);
    }

    [Fact]
    public async Task IfNoneMatch_with_wildcard_returns_304()
    {
        var (ctx, _) = await ExecuteAsync(ifNoneMatch: "*");

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status304NotModified);
    }

    [Fact]
    public async Task NonJsonContentType_does_not_set_etag()
    {
        var context = BuildContext();
        var middleware = new EtagMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "image/png";
            await ctx.Response.Body.WriteAsync(new byte[] { 1, 2, 3 });
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers.ETag.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task TextContentType_sets_etag()
    {
        var context = BuildContext();
        var middleware = new EtagMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.WriteAsync("hello");
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers.ETag.ToString().Should().NotBeEmpty();
    }

    [Fact]
    public async Task NonOkStatus_does_not_set_etag()
    {
        var context = BuildContext();
        var middleware = new EtagMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{}");
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers.ETag.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task SameBody_yields_same_etag_across_requests()
    {
        var (first, _) = await ExecuteAsync();
        var (second, _) = await ExecuteAsync();

        first.Response.Headers.ETag.ToString().Should().Be(second.Response.Headers.ETag.ToString());
    }

    [Fact]
    public async Task DifferentBodies_yield_different_etags()
    {
        var (first, _) = await ExecuteAsync(body: "{\"value\":1}");
        var (second, _) = await ExecuteAsync(body: "{\"value\":2}");

        first.Response.Headers.ETag.ToString().Should().NotBe(second.Response.Headers.ETag.ToString());
    }

    [Fact]
    public void Constructor_throws_when_next_is_null()
    {
        var act = () => new EtagMiddleware(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task FileDownloadPath_is_skipped_and_streams_passthrough()
    {
        var context = BuildContext();
        context.Request.Path = "/api/v1/reports/123";
        var originalBody = new MemoryStream();
        context.Response.Body = originalBody;
        var payload = new byte[] { 1, 2, 3, 4, 5 };

        var middleware = new EtagMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/pdf";
            await ctx.Response.Body.WriteAsync(payload);
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers.ETag.ToString().Should().BeEmpty();
        originalBody.ToArray().Should().BeEquivalentTo(payload);
    }

    [Fact]
    public async Task RangeRequest_is_skipped_and_streams_passthrough()
    {
        var context = BuildContext();
        context.Request.Path = "/api/v1/customers/abc";
        context.Request.Headers["Range"] = "bytes=0-99";
        var originalBody = new MemoryStream();
        context.Response.Body = originalBody;

        var middleware = new EtagMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status206PartialContent;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"chunk\":1}");
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers.ETag.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task LargeBody_above_threshold_does_not_get_etag()
    {
        var context = BuildContext();
        var originalBody = new MemoryStream();
        context.Response.Body = originalBody;
        var oversized = new string('a', (int)(1024 * 1024 + 16));

        var middleware = new EtagMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(oversized);
        });

        await middleware.InvokeAsync(context);

        context.Response.Headers.ETag.ToString().Should().BeEmpty();
        originalBody.Length.Should().Be(oversized.Length);
    }

    private static DefaultHttpContext BuildContext(string method = "GET")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<(DefaultHttpContext ctx, byte[] body)> ExecuteAsync(
        string body = "{\"value\":1}",
        string? ifNoneMatch = null)
    {
        var context = BuildContext();
        if (!string.IsNullOrEmpty(ifNoneMatch))
        {
            context.Request.Headers["If-None-Match"] = ifNoneMatch;
        }

        var originalBody = new MemoryStream();
        context.Response.Body = originalBody;

        var middleware = new EtagMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(body, Encoding.UTF8);
        });

        await middleware.InvokeAsync(context);

        return (context, originalBody.ToArray());
    }
}
