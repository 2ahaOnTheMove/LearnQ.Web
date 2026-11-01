using System.Security.Claims;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace LearnQ.Presentation.Security;

public sealed class AuthService : IAuthService
{
    private readonly IAuthApi _authApi;
    private readonly IRedisSessionStore _sessionStore;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        IAuthApi authApi,
        IRedisSessionStore sessionStore,
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

        var principal = CreatePrincipal(me, sid, rememberMe);
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

        var sid = context.User.FindFirst(AppClaims.Sid)?.Value;
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
        var isPersistent = context.User.FindFirst("is_persistent")?.Value == "true";
        var ttl = isPersistent ? TimeSpan.FromDays(30) : TimeSpan.FromHours(8);

        var principal = CreatePrincipal(me, sid, isPersistent);
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
        var sid = httpContext.User.FindFirst(AppClaims.Sid)?.Value;
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

    private static ClaimsPrincipal CreatePrincipal(CurrentUser me, string sid, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, me.Id),
            new("sub", me.Id),
            new(ClaimTypes.Name, me.DisplayName),
            new(ClaimTypes.Email, me.Email),
            new(AppClaims.EmailVerified, me.EmailVerified ? "true" : "false"),
            new(AppClaims.OnboardingCompleted, me.OnboardingCompleted ? "true" : "false"),
            new(AppClaims.Sid, sid),
            new("is_persistent", rememberMe ? "true" : "false")
        };

        if (me.Role.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.Role, me.Role.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(me.OnboardingStatus))
        {
            claims.Add(new Claim(AppClaims.OnboardingStatus, me.OnboardingStatus));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
