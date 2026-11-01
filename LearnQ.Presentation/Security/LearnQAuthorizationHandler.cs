using LearnQ.Application.Common.Constants;
using LearnQ.Presentation.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace LearnQ.Presentation.Security;

public sealed class LearnQAuthorizationHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        if (authorizeResult.Challenged)
        {
            var returnUrl = context.Request.Path + context.Request.QueryString;
            var safeReturnUrl = ReturnUrlValidator.GetSafeReturnUrl(returnUrl);
            var redirectUrl = string.IsNullOrEmpty(safeReturnUrl) || safeReturnUrl == AppRoutes.Home
                ? AppRoutes.Login
                : $"{AppRoutes.Login}?returnUrl={Uri.EscapeDataString(safeReturnUrl)}";

            context.Response.Redirect(redirectUrl);
            return;
        }

        if (authorizeResult.Forbidden)
        {
            var user = context.User;
            var isEmailVerified = user.FindFirst(AppClaims.EmailVerified)?.Value == "true";
            if (!isEmailVerified)
            {
                context.Response.Redirect(AppRoutes.VerifyEmail);
                return;
            }

            var isOnboardingCompleted = user.FindFirst(AppClaims.OnboardingCompleted)?.Value == "true";

            // If the failure was due to not completing onboarding
            if (!isOnboardingCompleted)
            {
                var destination = PostAuthRoute.GetPostAuthDestination(user);
                context.Response.Redirect(destination);
                return;
            }

            // Failing a role policy (e.g. Student trying to access Teacher area or vice versa)
            var postAuth = PostAuthRoute.GetPostAuthDestination(user);
            var accessDeniedUrl = postAuth.Contains('?')
                ? $"{postAuth}&notice={NoticeKeys.AccessDenied}"
                : $"{postAuth}?notice={NoticeKeys.AccessDenied}";

            context.Response.Redirect(accessDeniedUrl);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
