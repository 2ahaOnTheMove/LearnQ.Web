using System.Threading.RateLimiting;
using LearnQ.Presentation.Middleware;
using LearnQ.Presentation.Notices;
using LearnQ.Presentation.Routing;
using Microsoft.AspNetCore.HttpOverrides;

namespace LearnQ.Presentation.Startup;

public static class SecurityExtensions
{
    public static IServiceCollection AddAppSecurity(this IServiceCollection services, IConfiguration config)
    {
        // Forwarded headers for reverse proxies (client IP resolution)
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
#pragma warning disable ASPDEPR005 // KnownNetworks is obsolete in .NET 10, use KnownIPNetworks
            // In development, trust all proxies (localhost); in production, configure known proxy CIDRs
            if (config.GetValue<bool>("ForwardedHeaders:TrustLocalhost", true))
            {
                options.KnownNetworks.Clear();
            }
#pragma warning restore ASPDEPR005
        });

        // Rate Limiting — edge layer (Blazor BFF)
        // The Spring API has its own Redis-backed rate limiter; this is defence-in-depth.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                var httpContext = context.HttpContext;
                var request = httpContext.Request;
                var response = httpContext.Response;

                // Calculate retry seconds from the rate limiter lease metadata
                var retrySeconds = 30;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retrySeconds = Math.Clamp((int)Math.Ceiling(retryAfter.TotalSeconds), 1, 3600);
                }

                // Determine the scope from the request path
                var scope = request.Path switch
                {
                    var p when p.StartsWithSegments("/auth/login") => "auth.login",
                    var p when p.StartsWithSegments("/auth/verify-email") && HttpMethods.IsPost(request.Method) => "auth.verify",
                    var p when p.StartsWithSegments("/auth/verify-email?action=resend") => "auth.resend",
                    _ => "unknown"
                };

                // Set Retry-After header
                response.Headers.RetryAfter = retrySeconds.ToString();
                response.Headers.CacheControl = "no-store";

                // API endpoints return JSON 429
                if (request.Path.StartsWithSegments("/api"))
                {
                    response.ContentType = "application/json";
                    var body = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        type = "about:blank",
                        title = "Too Many Requests",
                        status = 429,
                        detail = "Too many requests. Please try again later.",
                        code = "RATE_LIMITED",
                        scope = scope,
                        retryAfterSeconds = retrySeconds
                    });
                    await response.WriteAsync(body, token);
                    await ValueTask.CompletedTask;
                    return;
                }

                // For GET requests, never redirect — that would create a redirect loop
                // (the redirected GET would be rate-limited again). Instead, return 429
                // with the notice query params so the page can render the modal inline.
                if (HttpMethods.IsGet(request.Method))
                {
                    // Check if the page already has a notice — if so, don't redirect at all,
                    // just return 429 so the page renders with the existing notice.
                    var existingNotice = request.Query["notice"].ToString();
                    if (!string.IsNullOrEmpty(existingNotice))
                    {
                        // Already showing a notice — just return 429, no redirect
                        response.StatusCode = StatusCodes.Status429TooManyRequests;
                        await ValueTask.CompletedTask;
                        return;
                    }

                    // Redirect to the same path with notice params, but only if the path
                    // doesn't already have them. This is safe because the check above
                    // catches the case where it does.
                    var getTargetPath = request.Path.Value ?? "/";
                    var getParts = new[]
                    {
                        $"notice={NoticeCatalog.NoticeKeys.TooManyRequests}",
                        $"retry={retrySeconds}",
                        $"scope={Uri.EscapeDataString(scope)}"
                    };
                    response.StatusCode = StatusCodes.Status303SeeOther;
                    response.Headers.Location = $"{getTargetPath}?{string.Join("&", getParts)}";
                    await ValueTask.CompletedTask;
                    return;
                }

                // For POST/PUT/DELETE, use post-redirect-get back to the referer page
                var referer = request.Headers.Referer.ToString();
                string postTargetPath;
                if (!string.IsNullOrEmpty(referer))
                {
                    if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
                    {
                        var path = refererUri.AbsolutePath;
                        if (ReturnUrlValidator.IsSafeLocalUrl(path))
                        {
                            postTargetPath = path;
                        }
                        else
                        {
                            postTargetPath = AppRoutes.Login;
                        }
                    }
                    else
                    {
                        postTargetPath = AppRoutes.Login;
                    }
                }
                else
                {
                    postTargetPath = AppRoutes.Login;
                }

                var returnUrl = request.Query["returnUrl"].ToString();
                var safeReturnUrl = ReturnUrlValidator.GetSafeReturnUrl(returnUrl);

                var postParts = new List<string>
                {
                    $"notice={NoticeCatalog.NoticeKeys.TooManyRequests}",
                    $"retry={retrySeconds}",
                    $"scope={Uri.EscapeDataString(scope)}"
                };
                if (!string.IsNullOrEmpty(safeReturnUrl))
                {
                    postParts.Add($"returnUrl={Uri.EscapeDataString(safeReturnUrl)}");
                }

                response.StatusCode = StatusCodes.Status303SeeOther;
                var redirectUrl = $"{postTargetPath}?{string.Join("&", postParts)}";
                response.Headers.Location = redirectUrl;

                await ValueTask.CompletedTask;
            };

            // Login: 20 per 5 min per IP (generous — Spring enforces per-account limits)
            options.AddPolicy(RateLimitPolicies.Login, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = config.GetValue<int>("RateLimiting:Login:PermitLimit", 20),
                        Window = TimeSpan.FromMinutes(config.GetValue<int>("RateLimiting:Login:WindowMinutes", 5)),
                        SegmentsPerWindow = 10,
                        QueueLimit = 0
                    }));

            // Verify email: 20 per 5 min per IP (generous — Spring enforces per-client limits)
            options.AddPolicy(RateLimitPolicies.VerifyEmail, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = config.GetValue<int>("RateLimiting:VerifyEmail:PermitLimit", 20),
                        Window = TimeSpan.FromMinutes(config.GetValue<int>("RateLimiting:VerifyEmail:WindowMinutes", 5)),
                        SegmentsPerWindow = 10,
                        QueueLimit = 0
                    }));

            // Resend verification: 10 per 5 min per IP (generous — Spring enforces cooldown/caps)
            options.AddPolicy(RateLimitPolicies.ResendVerification, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = config.GetValue<int>("RateLimiting:ResendVerification:PermitLimit", 10),
                        Window = TimeSpan.FromMinutes(config.GetValue<int>("RateLimiting:ResendVerification:WindowMinutes", 5)),
                        SegmentsPerWindow = 10,
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    public static IApplicationBuilder UseAppSecurity(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseForwardedHeaders();

        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler(AppRoutes.Error, createScopeForErrors: true);
            app.UseHsts();
        }

        // Do NOT use UseStatusCodePagesWithReExecute with Blazor.
        // Blazor handles its own 404/403 responses through the Router's NotFoundPage
        // and AuthorizeRouteView's NotAuthorized block. The middleware would intercept
        // those status codes and re-execute against /not-found, breaking Blazor routing.
        app.UseHttpsRedirection();

        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Rate limiter BEFORE static files so static assets are excluded by policy
        app.UseRateLimiter();

        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        return app;
    }
}
