using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Features.Auth;
using LearnQ.Presentation.Components;
using LearnQ.Presentation.Endpoints;
using LearnQ.Presentation.Routing;
using Microsoft.AspNetCore.Mvc;

namespace LearnQ.Presentation.Startup;

public static class WebApplicationExtensions
{
    public static IApplicationBuilder UseAppPipeline(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAppSecurity(env);
        return app;
    }

    public static IEndpointRouteBuilder MapAppEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapStaticAssets().AllowAnonymous();
        routes.MapLoginEndpoint();
        routes.MapLogoutEndpoint();
        routes.MapRegisterEndpoint();
        routes.MapApiLogoutEndpoint();
        routes.MapApiMeEndpoint();
        routes.MapApiRefreshEndpoint();
        routes.MapApiVerifyEmailEndpoint();
        routes.MapApiResendVerificationEndpoint();
        routes.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AllowAnonymous();
        return routes;
    }
}

public static class RegisterEndpoint
{
    public static void MapRegisterEndpoint(this IEndpointRouteBuilder routes)
    {
        // POST /auth/register — browser form POST for client registration.
        // The backend (Spring Boot) returns 201 Created with user info but NO tokens.
        // The user must log in to obtain access/refresh tokens.
        // See: docs/auth-api-contract.md section 3.1
        // Requires antiforgery because it's a browser-originated cookie-authenticated request.
        routes.MapPost(AppRoutes.Register, async (
                [FromForm] string givenName,
                [FromForm] string familyName,
                [FromForm] string displayName,
                [FromForm] string email,
                [FromForm] string password,
                [FromForm] bool termsAccepted,
                [FromForm] bool privacyAccepted,
                IClientApi clientApi,
                CancellationToken cancellationToken) =>
            {
                var request = new RegisterClientRequest(
                    givenName, familyName, displayName, email, password, termsAccepted, privacyAccepted);

                var result = await clientApi.RegisterAsync(request, cancellationToken);
                if (result.IsFailure || result.Value is null)
                {
                    var error = result.Error;
                    return Results.Redirect($"{AppRoutes.Register}?registerError={error?.Code ?? "REGISTRATION_FAILED"}&registerMessage={Uri.EscapeDataString(error?.Message ?? "Registration failed.")}");
                }

                // Registration succeeded. The backend does not issue tokens on registration.
                // Redirect the user to the login page with a success message so they can
                // sign in and obtain their authentication tokens.
                return Results.Redirect($"{AppRoutes.Login}?registerSuccess=true");
            })
            .AllowAnonymous()
            .WithName("Register");
    }
}
