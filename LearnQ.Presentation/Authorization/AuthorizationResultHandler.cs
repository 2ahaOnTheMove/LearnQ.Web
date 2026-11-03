using LearnQ.Application.Common.Constants;
using LearnQ.Presentation.Notices;
using LearnQ.Presentation.Routing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace LearnQ.Presentation.Authorization;

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
            // Only redirect to login for page navigation (HTML requests).
            // Static assets, API calls, etc. should return 401, not a redirect.
            if (!context.Request.AcceptsHtml())
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            // Never redirect to login if we're already on the login or register page — that would loop.
            // For verify-email, an unauthenticated user should be redirected to login (they need to log in first).
            if (context.Request.Path.StartsWithSegments(AppRoutes.Login)
                || context.Request.Path.StartsWithSegments(AppRoutes.Register))
            {
                // User is already on an auth page but not authenticated — this means
                // the cookie expired or was invalid. Clear the stale cookie and let
                // the page render as anonymous.
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    // Stale cookie — sign out to clear it
                    await context.SignOutAsync();
                }

                // Re-execute the request so the page renders as anonymous
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var returnUrl = context.Request.Path + context.Request.QueryString;
            var safeReturnUrl = ReturnUrlValidator.GetSafeReturnUrl(returnUrl);

            // Don't add returnUrl if it's an auth page or home — avoid pointless redirects
            if (!string.IsNullOrEmpty(safeReturnUrl)
                && !safeReturnUrl.StartsWith("/auth/login")
                && !safeReturnUrl.StartsWith("/auth/register")
                && !safeReturnUrl.StartsWith("/auth/verify-email")
                && safeReturnUrl != AppRoutes.Home)
            {
                context.Response.Redirect($"{AppRoutes.Login}?returnUrl={Uri.EscapeDataString(safeReturnUrl!)}");
            }
            else
            {
                context.Response.Redirect(AppRoutes.Login);
            }

            return;
        }

        if (authorizeResult.Forbidden)
        {
            // Never redirect to verify-email if we're already there
            if (context.Request.Path.StartsWithSegments(AppRoutes.VerifyEmail))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            var user = context.User;
            var isEmailVerified = user.FindFirst(AuthClaimTypes.EmailVerified)?.Value == "true";
            if (!isEmailVerified)
            {
                context.Response.Redirect(AppRoutes.VerifyEmail);
                return;
            }

            var isOnboardingCompleted = user.FindFirst(AuthClaimTypes.OnboardingCompleted)?.Value == "true";

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
                ? $"{postAuth}&notice={NoticeCatalog.NoticeKeys.AccessDenied}"
                : $"{postAuth}?notice={NoticeCatalog.NoticeKeys.AccessDenied}";

            context.Response.Redirect(accessDeniedUrl);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
