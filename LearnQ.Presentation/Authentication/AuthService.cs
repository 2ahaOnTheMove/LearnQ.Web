using LearnQ.Application.Common.Constants;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;
using LearnQ.Application.Features.Auth.Session;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LearnQ.Presentation.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly IAuthApi _authApi;
    private readonly ISessionTokenStore _sessionStore;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        IAuthApi authApi,
        ISessionTokenStore sessionStore,
        IHttpContextAccessor httpContextAccessor)
    {
        _authApi = authApi;
        _sessionStore = sessionStore;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<CurrentUser>> SignInAsync(
        string identifier,
        string password,
        bool rememberMe,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var loginResult = await _authApi.LoginAsync(new LoginRequest(identifier, password, rememberMe), cancellationToken);
        if (loginResult.IsFailure || loginResult.Value is null)
        {
            return Result<CurrentUser>.Failure(loginResult.Error ?? new ApiError("UNAUTHORIZED", "Invalid email/username or password", 401));
        }

        var loginResponse = loginResult.Value;
        var sid = Guid.NewGuid().ToString("N");
        var ttl = rememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromHours(8);

        await _sessionStore.SetTokensAsync(
            sid,
            new SessionTokenData(loginResponse.AccessToken, loginResponse.RefreshToken, loginResponse.ExpiresAt),
            ttl,
            cancellationToken);

        // Retrieve /me
        var meResult = await _authApi.GetMeAsync(cancellationToken);
        CurrentUser me;
        if (meResult.IsSuccess && meResult.Value is not null)
        {
            me = meResult.Value;
        }
        else
        {
            me = new CurrentUser(
                Id: "usr_" + sid[..8],
                Email: identifier,
                DisplayName: identifier.Contains('@') ? identifier.Split('@')[0] : identifier,
                Role: null,
                EmailVerified: false,
                OnboardingCompleted: false,
                OnboardingStatus: null);
        }

        var principal = ClaimsFactory.CreatePrincipal(me, sid, rememberMe);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(ttl),
            AllowRefresh = true
        };

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        return Result<CurrentUser>.Success(me);
    }

    public async Task<Result<CurrentUser>> RefreshPrincipalAsync(
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = httpContext ?? _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated != true)
        {
            return Result<CurrentUser>.Failure(new ApiError("UNAUTHORIZED", "User is not authenticated.", 401));
        }

        var sid = context.User.FindFirst(AuthClaimTypes.Sid)?.Value;
        if (string.IsNullOrEmpty(sid))
        {
            return Result<CurrentUser>.Failure(new ApiError("SESSION_EXPIRED", "Session identifier missing.", 401));
        }

        var meResult = await _authApi.GetMeAsync(cancellationToken);
        if (meResult.IsFailure || meResult.Value is null)
        {
            return meResult;
        }

        var me = meResult.Value;
        var isPersistent = context.User.FindFirst(AuthClaimTypes.IsPersistent)?.Value == "true";
        var ttl = isPersistent ? TimeSpan.FromDays(30) : TimeSpan.FromHours(8);

        var principal = ClaimsFactory.CreatePrincipal(me, sid, isPersistent);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(ttl),
            AllowRefresh = true
        };

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        return Result<CurrentUser>.Success(me);
    }

    public async Task SignOutAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var sid = httpContext.User.FindFirst(AuthClaimTypes.Sid)?.Value;
        if (!string.IsNullOrEmpty(sid))
        {
            var tokens = await _sessionStore.GetTokensAsync(sid, cancellationToken);
            if (tokens is not null)
            {
                _ = _authApi.LogoutAsync(new RefreshTokenRequest(tokens.RefreshToken), cancellationToken);
            }

            await _sessionStore.InvalidateSessionAsync(sid, cancellationToken);
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
