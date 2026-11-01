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

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(AuthClaimTypes.EmailVerified, "true")
                .Build();
        });

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, LearnQAuthorizationHandler>();

        return services;
    }
}
