using LearnQ.Presentation.Authentication;
using LearnQ.Presentation.Routing;
using Microsoft.AspNetCore.Mvc;

namespace LearnQ.Presentation.Endpoints;

public static class ApiLogoutEndpoint
{
    public static void MapApiLogoutEndpoint(this IEndpointRouteBuilder routes)
    {
        routes.MapPost(AppRoutes.ApiLogout, async (
                HttpContext httpContext,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                await authService.SignOutAsync(httpContext, cancellationToken);
                return Results.Json(new
                {
                    success = true,
                    message = "Logged out successfully"
                });
            })
            .DisableAntiforgery()
            .RequireAuthorization()
            .WithName("ApiLogout");
    }
}
