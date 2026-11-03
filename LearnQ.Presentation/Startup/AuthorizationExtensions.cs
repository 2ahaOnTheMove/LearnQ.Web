using LearnQ.Application.Common.Constants;
using LearnQ.Application.Common.Enums;
using LearnQ.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace LearnQ.Presentation.Startup;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.AuthenticatedOnly, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(AuthPolicies.EmailVerified, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim(AuthClaimTypes.EmailVerified, "true"));

            options.AddPolicy(AuthPolicies.OnboardingCompleted, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim(AuthClaimTypes.EmailVerified, "true")
                      .RequireClaim(AuthClaimTypes.OnboardingCompleted, "true"));

            options.AddPolicy(AuthPolicies.StudentOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim(AuthClaimTypes.EmailVerified, "true")
                      .RequireRole(UserRole.Student.ToString()));

            options.AddPolicy(AuthPolicies.TeacherOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim(AuthClaimTypes.EmailVerified, "true")
                      .RequireRole(UserRole.Teacher.ToString()));

            // Do NOT set a restrictive FallbackPolicy.
            // A fallback policy requiring authenticated users would block all endpoints
            // without explicit authorization metadata, including login and register POST endpoints.
            // Instead, authorization is enforced explicitly:
            // - MapRazorComponents<App>() uses the custom IAuthorizationMiddlewareResultHandler
            //   to handle Blazor-side authorization via [Authorize] and AuthorizeRouteView.
            // - Protected API endpoints use .RequireAuthorization().
            // - Auth endpoints (login, register) use .AllowAnonymous().
            // - The custom handler redirects unauthenticated users to login for HTML requests.
        });

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, LearnQAuthorizationHandler>();

        return services;
    }
}
