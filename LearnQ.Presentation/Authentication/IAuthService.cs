using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;

namespace LearnQ.Presentation.Authentication;

public interface IAuthService
{
    Task<Result<CurrentUser>> SignInAsync(
        string identifier,
        string password,
        bool rememberMe,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task<Result<CurrentUser>> RefreshPrincipalAsync(
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task<Result<CurrentUser>> GetCurrentUserAsync(
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default);

    Task<Result<CurrentUser>> RefreshTokenAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task<Result> VerifyEmailAsync(
        string code,
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default);

    Task<Result<ResendVerificationResponse>> ResendVerificationAsync(
        CancellationToken cancellationToken = default);
}
