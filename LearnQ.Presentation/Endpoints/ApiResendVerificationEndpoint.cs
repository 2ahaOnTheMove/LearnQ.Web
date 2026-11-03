using LearnQ.Presentation.Authentication;
using LearnQ.Presentation.Routing;
using Microsoft.AspNetCore.RateLimiting;

namespace LearnQ.Presentation.Endpoints;

public static class ApiResendVerificationEndpoint
{
    public static void MapApiResendVerificationEndpoint(this IEndpointRouteBuilder routes)
    {
        routes.MapPost(AppRoutes.ApiResendVerification, async (
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.ResendVerificationAsync(cancellationToken);

                if (result.IsFailure)
                {
                    return Results.Json(new
                    {
                        success = false,
                        error = result.Error?.Code ?? "RESEND_VERIFICATION_FAILED",
                        message = result.Error?.Message ?? "Failed to resend verification email."
                    }, statusCode: 400);
                }

                var response = result.Value!;
                return Results.Json(new
                {
                    success = true,
                    message = "Verification email sent",
                    retryAfterSeconds = response.RetryAfterSeconds
                });
            })
            .DisableAntiforgery()
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.ResendVerification)
            .WithName("ApiResendVerification");
    }
}
