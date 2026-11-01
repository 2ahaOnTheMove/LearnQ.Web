using LearnQ.Application.Common.Models;

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
}
