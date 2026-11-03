using LearnQ.Presentation.Authentication;
using LearnQ.Presentation.Routing;
using Microsoft.AspNetCore.Mvc;

namespace LearnQ.Presentation.Endpoints;

public static class ApiRefreshEndpoint
{
    public static void MapApiRefreshEndpoint(this IEndpointRouteBuilder routes)
    {
        routes.MapPost(AppRoutes.ApiRefresh, async (
                HttpContext httpContext,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.RefreshTokenAsync(httpContext, cancellationToken);

                if (result.IsFailure)
                {
                    return Results.Json(new
                    {
                        success = false,
                        error = result.Error?.Code ?? "REFRESH_FAILED",
                        message = result.Error?.Message ?? "Failed to refresh session."
                    }, statusCode: 401);
                }

                var user = result.Value!;
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
                    }
                });
            })
            .DisableAntiforgery()
            .RequireAuthorization()
            .WithName("ApiRefresh");
    }
}
