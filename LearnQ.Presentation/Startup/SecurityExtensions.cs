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
        });

        // Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status303SeeOther;
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

                // Set Retry-After header
                response.Headers.RetryAfter = retrySeconds.ToString();

                // Determine the target path
                string targetPath;
                if (HttpMethods.IsGet(request.Method))
                {
                    targetPath = request.Path.Value ?? "/";
                }
                else
                {
                    var referer = request.Headers.Referer.ToString();
                    if (!string.IsNullOrEmpty(referer))
                    {
                        if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
                        {
                            var path = refererUri.AbsolutePath;
                            if (ReturnUrlValidator.IsSafeLocalUrl(path))
                            {
                                targetPath = path;
                            }
                            else
                            {
                                targetPath = AppRoutes.Login;
                            }
                        }
                        else
                        {
                            targetPath = AppRoutes.Login;
                        }
                    }
                    else
                    {
                        targetPath = AppRoutes.Login;
                    }
                }

                var returnUrl = request.Query["returnUrl"].ToString();
                var safeReturnUrl = ReturnUrlValidator.GetSafeReturnUrl(returnUrl);

                var parts = new List<string>
                {
                    $"notice={NoticeCatalog.NoticeKeys.TooManyRequests}",
                    $"retry={retrySeconds}"
                };
                if (!string.IsNullOrEmpty(safeReturnUrl))
                {
                    parts.Add($"returnUrl={Uri.EscapeDataString(safeReturnUrl)}");
                }

                var redirectUrl = $"{targetPath}?{string.Join("&", parts)}";
                response.Headers.Location = redirectUrl;

                await ValueTask.CompletedTask;
            };

            options.AddPolicy(RateLimitPolicies.Login, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = config.GetValue<int>("RateLimiting:Login:PermitLimit", 10),
                        Window = TimeSpan.FromMinutes(config.GetValue<int>("RateLimiting:Login:WindowMinutes", 1)),
                        QueueLimit = 0
                    }));

            options.AddPolicy(RateLimitPolicies.VerifyEmail, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = config.GetValue<int>("RateLimiting:VerifyEmail:PermitLimit", 10),
                        Window = TimeSpan.FromMinutes(config.GetValue<int>("RateLimiting:VerifyEmail:WindowMinutes", 1)),
                        QueueLimit = 0
                    }));

            options.AddPolicy(RateLimitPolicies.ResendVerification, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = config.GetValue<int>("RateLimiting:ResendVerification:PermitLimit", 3),
                        Window = TimeSpan.FromMinutes(config.GetValue<int>("RateLimiting:ResendVerification:WindowMinutes", 1)),
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

        app.UseStatusCodePagesWithReExecute(AppRoutes.NotFound, createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseRateLimiter();

        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        return app;
    }
}
