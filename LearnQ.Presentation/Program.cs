using System.Threading.RateLimiting;
using LearnQ.Application.Common.Constants;
using LearnQ.Application.Common.Enums;
using LearnQ.Infrastructure;
using LearnQ.Presentation.Components;
using LearnQ.Presentation.Routing;
using LearnQ.Presentation.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Forwarded headers for reverse proxies (client IP resolution)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Redis connection
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
var redis = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// Distributed cache backed by Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "learnq:";
});

// Data Protection persisted in Redis
builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
    .SetApplicationName("LearnQ");

// Redis-backed ITicketStore and Session Store
builder.Services.AddSingleton<RedisTicketStore>();
builder.Services.AddSingleton<ITicketStore>(sp => sp.GetRequiredService<RedisTicketStore>());
builder.Services.AddSingleton<IRedisSessionStore, RedisSessionStore>();

// Authentication & Session Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();

// Cookie Authentication
var isDevelopment = builder.Environment.IsDevelopment();
var cookieName = isDevelopment ? "learnq-session" : "__Host-learnq-session";

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = cookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = AppRoutes.Login;
        options.LogoutPath = AppRoutes.Logout;
        options.AccessDeniedPath = AppRoutes.Login;
    });

builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore>((options, ticketStore) =>
    {
        options.SessionStore = ticketStore;
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.AuthenticatedOnly, policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy(AuthPolicies.EmailVerified, policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim(AppClaims.EmailVerified, "true"));

    options.AddPolicy(AuthPolicies.OnboardingCompleted, policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim(AppClaims.EmailVerified, "true")
              .RequireClaim(AppClaims.OnboardingCompleted, "true"));

    options.AddPolicy(AuthPolicies.StudentOnly, policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim(AppClaims.EmailVerified, "true")
              .RequireRole(UserRole.Student.ToString()));

    options.AddPolicy(AuthPolicies.TeacherOnly, policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim(AppClaims.EmailVerified, "true")
              .RequireRole(UserRole.Teacher.ToString()));

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim(AppClaims.EmailVerified, "true")
        .Build();
});

builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, LearnQAuthorizationHandler>();

// Blazor Authentication State Provider
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingAuthStateProvider>();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status302Found;
    options.OnRejected = (context, token) =>
    {
        var request = context.HttpContext.Request;
        var path = request.Path.Value ?? "/";
        var redirectUrl = path.Contains('?')
            ? $"{path}&notice={NoticeKeys.TooManyRequests}"
            : $"{path}?notice={NoticeKeys.TooManyRequests}";

        context.HttpContext.Response.Headers.Location = redirectUrl;
        return ValueTask.CompletedTask;
    };

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:Login:PermitLimit", 10),
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("RateLimiting:Login:WindowMinutes", 1)),
                QueueLimit = 0
            }));

    options.AddPolicy("verify-email", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:VerifyEmail:PermitLimit", 10),
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("RateLimiting:VerifyEmail:WindowMinutes", 1)),
                QueueLimit = 0
            }));

    options.AddPolicy("resend-verification", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:ResendVerification:PermitLimit", 3),
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("RateLimiting:ResendVerification:WindowMinutes", 1)),
                QueueLimit = 0
            }));
});

// Infrastructure & Bearer DelegatingHandler
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddInfrastructure(
    builder.Configuration,
    clientBuilder => clientBuilder.AddHttpMessageHandler<BearerTokenHandler>());

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(AppRoutes.Error, createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(AppRoutes.NotFound, createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// Antiforgery-protected Logout endpoint for form submission
app.MapPost(AppRoutes.Logout, async (HttpContext httpContext, IAuthService authService) =>
{
    await authService.SignOutAsync(httpContext);
    return Results.Redirect(AppRoutes.Login);
}).RequireAuthorization(AuthPolicies.AuthenticatedOnly);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
