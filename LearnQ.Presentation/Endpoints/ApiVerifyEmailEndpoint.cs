using LearnQ.Presentation.Authentication;
using LearnQ.Presentation.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LearnQ.Presentation.Endpoints;

public static class ApiVerifyEmailEndpoint
{
    public static void MapApiVerifyEmailEndpoint(this IEndpointRouteBuilder routes)
    {
        routes.MapPost(AppRoutes.ApiVerifyEmail, async (
                [FromBody] VerifyEmailRequest request,
                HttpContext httpContext,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.VerifyEmailAsync(request.Code, httpContext, cancellationToken);

                if (result.IsFailure)
                {
                    return Results.Json(new
                    {
                        success = false,
                        error = result.Error?.Code ?? "VERIFY_EMAIL_FAILED",
                        message = result.Error?.Message ?? "Failed to verify email."
                    }, statusCode: 400);
                }

                return Results.Json(new
                {
                    success = true,
                    message = "Email verified successfully"
                });
            })
            .DisableAntiforgery()
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.VerifyEmail)
            .WithName("ApiVerifyEmail");
    }
}

public sealed class VerifyEmailRequest
{
    public string Code { get; set; } = string.Empty;
}
