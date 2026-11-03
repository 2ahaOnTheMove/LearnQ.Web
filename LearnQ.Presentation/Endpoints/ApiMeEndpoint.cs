using LearnQ.Presentation.Authentication;
using LearnQ.Presentation.Routing;
using Microsoft.AspNetCore.Mvc;

namespace LearnQ.Presentation.Endpoints;

public static class ApiMeEndpoint
{
    public static void MapApiMeEndpoint(this IEndpointRouteBuilder routes)
    {
        routes.MapGet(AppRoutes.ApiMe, async (
                HttpContext httpContext,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.GetCurrentUserAsync(httpContext, cancellationToken);

                if (result.IsFailure)
                {
                    return Results.Json(new
                    {
                        success = false,
                        error = result.Error?.Code ?? "UNAUTHORIZED",
                        message = result.Error?.Message ?? "User is not authenticated."
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
            .WithName("ApiMe");
    }
}
