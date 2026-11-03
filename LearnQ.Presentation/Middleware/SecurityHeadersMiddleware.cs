namespace LearnQ.Presentation.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // HSTS (always, even in dev so local HTTPS works correctly)
            if ("https".Equals(context.Request.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            // Content-Security-Policy in Report-Only mode
            headers["Content-Security-Policy-Report-Only"] =
                "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' ws: wss:; frame-ancestors 'none';";

            // Cache-Control no-store on auth and API pages
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/auth") || path.StartsWith("/api"))
            {
                headers["Cache-Control"] = "no-store";
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
