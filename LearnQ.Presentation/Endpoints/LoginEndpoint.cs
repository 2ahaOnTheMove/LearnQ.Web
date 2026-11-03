using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;
using LearnQ.Presentation.Authentication;
using LearnQ.Presentation.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LearnQ.Presentation.Endpoints;

public static class LoginEndpoint
{
    public static void MapLoginEndpoint(this IEndpointRouteBuilder routes)
    {
        // POST /auth/login — browser form POST that sets the authentication cookie.
        // This is the primary login mechanism: the browser submits a real HTTP POST,
        // ASP.NET Core signs in and the response Set-Cookie header establishes the session.
        routes.MapPost(AppRoutes.Login, async (
                [FromForm] string identifier,
                [FromForm] string password,
                [FromQuery] string? returnUrl,
                HttpContext httpContext,
                IAuthService authService,
                CancellationToken cancellationToken,
                [FromForm] bool? rememberMe = false) =>
            {
                if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
                {
                    return Results.Redirect($"{AppRoutes.Login}?loginError=missing_credentials");
                }

                var result = await authService.SignInAsync(
                    identifier,
                    password,
                    rememberMe ?? false,
                    httpContext,
                    cancellationToken);

                if (result.IsFailure)
                {
                    return Results.Redirect($"{AppRoutes.Login}?loginError=invalid_credentials");
                }

                // Use the user's onboarding and verification status to determine
                // the correct post-login destination. New users go to onboarding,
                // verified users with completed onboarding go to the dashboard.
                var user = result.Value!;
                var destination = PostAuthRoute.GetPostAuthDestination(user, returnUrl);
                return Results.Redirect(destination);
            })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Login)
            .WithName("Login");

        // POST /api/auth/login — JSON endpoint for programmatic clients.
        // Also sets the authentication cookie on the response.
        routes.MapPost(AppRoutes.ApiLogin, async (
                [FromBody] LoginRequest request,
                [FromQuery] string? returnUrl,
                HttpContext httpContext,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.SignInAsync(
                    request.Identifier,
                    request.Password,
                    request.RememberMe,
                    httpContext,
                    cancellationToken);

                if (result.IsFailure)
                {
                    return Results.Json(new
                    {
                        success = false,
                        error = result.Error?.Code ?? "LOGIN_FAILED",
                        message = result.Error?.Message ?? "Invalid email/username or password"
                    }, statusCode: 401);
                }

                var user = result.Value!;
                var destination = PostAuthRoute.GetPostAuthDestination(user, returnUrl);
                return Results.Json(new
                {
                    success = true,
                    user = new
                    {
                        user.Id,
                        user.Email,
                        user.DisplayName,
                        user.Role,
                        user.EmailVerified,
                        user.OnboardingCompleted,
                        user.OnboardingStatus
                    },
                    returnUrl = destination
                });
            })
            .DisableAntiforgery()
            .RequireRateLimiting(RateLimitPolicies.Login)
            .WithName("ApiLogin");
    }
}
