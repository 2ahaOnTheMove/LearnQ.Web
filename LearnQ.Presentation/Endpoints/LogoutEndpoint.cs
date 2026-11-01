using LearnQ.Application.Common.Constants;
using LearnQ.Presentation.Authentication;
using LearnQ.Presentation.Routing;

namespace LearnQ.Presentation.Endpoints;

public static class LogoutEndpoint
{
    public static void MapLogoutEndpoint(this IEndpointRouteBuilder routes)
    {
        routes.MapPost(AppRoutes.Logout, async (HttpContext httpContext, IAuthService authService) =>
        {
            await authService.SignOutAsync(httpContext);
            return Results.Redirect(AppRoutes.Login);
        }).RequireAuthorization(AuthPolicies.AuthenticatedOnly);
    }
}
