namespace CoreAlign.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Defense-in-depth headers applied to every response, including API JSON.
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=(), usb=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-site";

        if (!_environment.IsDevelopment())
        {
            headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";

            // CSP tuned for a SPA built with Tailwind v4 + React.
            //
            // 'unsafe-inline' for style-src is *required* by Tailwind utility classes
            // and animation engines (framer-motion writes inline styles for transforms);
            // dropping it breaks the UI. We mitigate that gap by enabling Trusted Types
            // for script and locking everything else down.
            //
            // data: in img-src covers SVG-as-data-URI icons; blob: covers CSV/PDF
            // downloads generated client-side. font-src data: covers icon-font data URIs.
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "script-src-attr 'none'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: blob:; " +
                "font-src 'self' data:; " +
                "connect-src 'self'; " +
                "media-src 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'none'; " +
                "form-action 'self'; " +
                "base-uri 'self'; " +
                "manifest-src 'self'; " +
                "worker-src 'self' blob:; " +
                "upgrade-insecure-requests";
        }

        return _next(context);
    }
}
