using LearnQ.Application.Features.Auth.Session;
using LearnQ.Presentation.Authentication;
using LearnQ.Presentation.Routing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

namespace LearnQ.Presentation.Startup;

public static class AuthenticationExtensions
{
    // Cookie name constants
    private const string CookieNameDevelopment = "learnq-session";
    private const string CookieNameProduction = "__Host-learnq-session";

    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var environment = services.BuildServiceProvider().GetRequiredService<IHostEnvironment>();
        var isDevelopment = environment.IsDevelopment();
        var cookieName = isDevelopment ? CookieNameDevelopment : CookieNameProduction;

        services.AddHttpContextAccessor();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISessionAccessor, SessionAccessor>();

        // Cookie Authentication
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
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

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((options, ticketStore) =>
            {
                options.SessionStore = ticketStore;
            });

        // Blazor Authentication State Provider
        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, RevalidatingAuthStateProvider>();

        return services;
    }
}
