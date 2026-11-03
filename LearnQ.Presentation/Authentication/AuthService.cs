using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using LearnQ.Application.Common.Constants;
using LearnQ.Application.Common.Enums;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Common.Models;
using LearnQ.Application.Features.Auth;
using LearnQ.Application.Features.Auth.Session;
using LearnQ.Infrastructure.Api.Auth;
using LearnQ.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

namespace LearnQ.Presentation.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly IAuthApi _authApi;
    private readonly IAuthenticatedAuthApi _authenticatedAuthApi;
    private readonly ISessionTokenStore _sessionStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ApiOptions _apiOptions;

    public AuthService(
        IAuthApi authApi,
        IAuthenticatedAuthApi authenticatedAuthApi,
        ISessionTokenStore sessionStore,
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authStateProvider,
        ApiOptions apiOptions)
    {
        _authApi = authApi;
        _authenticatedAuthApi = authenticatedAuthApi;
        _sessionStore = sessionStore;
        _httpContextAccessor = httpContextAccessor;
        _authStateProvider = authStateProvider;
        _apiOptions = apiOptions;
    }

    /// <summary>
    /// Resolves the session ID from HttpContext first, then falls back to
    /// AuthenticationStateProvider for Blazor Interactive Server circuits where
    /// HttpContext is null.
    /// </summary>
    private async Task<string?> ResolveSessionIdAsync(CancellationToken cancellationToken = default)
    {
        // HttpContext is available during normal HTTP requests
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var sid = httpContext.User.FindFirst(AuthClaimTypes.Sid)?.Value;
            if (!string.IsNullOrEmpty(sid))
                return sid;
        }

        // In Blazor Interactive Server circuits, HttpContext is null — use AuthenticationStateProvider
        try
        {
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            if (state.User.Identity?.IsAuthenticated == true)
            {
                return state.User.FindFirst(AuthClaimTypes.Sid)?.Value;
            }
        }
        catch
        {
            // Circuit may not have initialized state yet
        }

        return null;
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

        // Retrieve /me using the access token directly, bypassing BearerTokenHandler.
        // BearerTokenHandler reads the session from HttpContext.User, but the cookie
        // hasn't been set yet at this point, so it would send the request without a
        // token and get 401 from the backend.
        CurrentUser me = await FetchMeWithTokenAsync(loginResponse.AccessToken, cancellationToken)
            ?? new CurrentUser(
                Id: "usr_" + sid[..8],
                Email: identifier,
                DisplayName: identifier.Contains('@') ? identifier.Split('@')[0] : identifier,
                Role: null,
                EmailVerified: false,
                OnboardingCompleted: false,
                OnboardingStatus: null);

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

        // Set AsyncLocal session ID so BearerTokenHandler can access it across DI scope boundaries.
        SessionContext.SessionId = sid;
        try
        {
            // Try to fetch /me from the backend. If the backend rejects the token
            // (e.g. AccountState not yet ACTIVE for newly registered users), fall back
            // to the claims already present in the cookie rather than failing the request.
            var meResult = await _authenticatedAuthApi.GetMeAsync(cancellationToken);
            CurrentUser? me = meResult.Value;
            if (me is null && context.User.Identity is not null)
            {
                // Build CurrentUser from existing cookie claims as a fallback.
                // This handles the case where the backend /me endpoint rejects the token
                // because the account state is not yet fully initialized (e.g. PENDING status).
                var roleStr = context.User.FindFirst(ClaimTypes.Role)?.Value;
                UserRole? role = Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var parsed) ? parsed : null;
                me = new CurrentUser(
                    Id: context.User.FindFirst(AuthClaimTypes.Sub)?.Value ?? "",
                    Email: context.User.FindFirst(AuthClaimTypes.Email)?.Value ?? "",
                    DisplayName: context.User.FindFirst(AuthClaimTypes.Name)?.Value ?? "",
                    Role: role,
                    EmailVerified: context.User.FindFirst(AuthClaimTypes.EmailVerified)?.Value == "true",
                    OnboardingCompleted: context.User.FindFirst(AuthClaimTypes.OnboardingCompleted)?.Value == "true",
                    OnboardingStatus: context.User.FindFirst(AuthClaimTypes.OnboardingStatus)?.Value);
            }

            var isPersistent = context.User.FindFirst(AuthClaimTypes.IsPersistent)?.Value == "true";
            var ttl = isPersistent ? TimeSpan.FromDays(30) : TimeSpan.FromHours(8);

            var principal = ClaimsFactory.CreatePrincipal(me!, sid, isPersistent);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(ttl),
                AllowRefresh = true
            };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
            return Result<CurrentUser>.Success(me ?? throw new InvalidOperationException("CurrentUser could not be resolved."));
        }
        finally
        {
            SessionContext.SessionId = null;
        }
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

    public async Task<Result<CurrentUser>> GetCurrentUserAsync(
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = httpContext ?? _httpContextAccessor.HttpContext;
        
        // Resolve SID from HttpContext or Blazor circuit's AuthenticationStateProvider
        var sid = context?.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(AuthClaimTypes.Sid)?.Value
            : await ResolveSessionIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(sid) && context?.User?.Identity?.IsAuthenticated != true)
        {
            return Result<CurrentUser>.Failure(new ApiError("UNAUTHORIZED", "User is not authenticated.", 401));
        }

        if (!string.IsNullOrEmpty(sid))
        {
            SessionContext.SessionId = sid;
        }

        try
        {
            // Try to fetch /me from the backend. If the backend rejects the token
            // (e.g. AccountState not yet ACTIVE), fall back to cookie claims.
            var meResult = await _authenticatedAuthApi.GetMeAsync(cancellationToken);
            CurrentUser? me = meResult.Value;
            if (me is null && context?.User?.Identity is not null)
            {
                var roleStr = context.User.FindFirst(ClaimTypes.Role)?.Value;
                UserRole? role = Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var parsed) ? parsed : null;
                me = new CurrentUser(
                    Id: context.User.FindFirst(AuthClaimTypes.Sub)?.Value ?? "",
                    Email: context.User.FindFirst(AuthClaimTypes.Email)?.Value ?? "",
                    DisplayName: context.User.FindFirst(AuthClaimTypes.Name)?.Value ?? "",
                    Role: role,
                    EmailVerified: context.User.FindFirst(AuthClaimTypes.EmailVerified)?.Value == "true",
                    OnboardingCompleted: context.User.FindFirst(AuthClaimTypes.OnboardingCompleted)?.Value == "true",
                    OnboardingStatus: context.User.FindFirst(AuthClaimTypes.OnboardingStatus)?.Value);
            }

            return Result<CurrentUser>.Success(me ?? throw new InvalidOperationException("CurrentUser could not be resolved."));
        }
        finally
        {
            SessionContext.SessionId = null;
        }
    }

    public async Task<Result<CurrentUser>> RefreshTokenAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return Result<CurrentUser>.Failure(new ApiError("UNAUTHORIZED", "User is not authenticated.", 401));
        }

        var sid = httpContext.User.FindFirst(AuthClaimTypes.Sid)?.Value;
        if (string.IsNullOrEmpty(sid))
        {
            return Result<CurrentUser>.Failure(new ApiError("SESSION_EXPIRED", "Session identifier missing.", 401));
        }

        SessionContext.SessionId = sid;
        try
        {
            // Try to refresh tokens via the backend
            var tokens = await _sessionStore.GetTokensAsync(sid, cancellationToken);
            if (tokens is not null)
            {
                var refreshResult = await _authApi.RefreshTokenAsync(
                    new RefreshTokenRequest(tokens.RefreshToken), cancellationToken);

                if (refreshResult.IsSuccess && refreshResult.Value is not null)
                {
                    var loginResponse = refreshResult.Value;
                    var isPersistentToken = httpContext.User.FindFirst(AuthClaimTypes.IsPersistent)?.Value == "true";
                    var ttlToken = isPersistentToken ? TimeSpan.FromDays(30) : TimeSpan.FromHours(8);

                    await _sessionStore.SetTokensAsync(
                        sid,
                        new SessionTokenData(loginResponse.AccessToken, loginResponse.RefreshToken, loginResponse.ExpiresAt),
                        ttlToken,
                        cancellationToken);
                }
            }

            // Re-sign in with fresh claims from backend, or fall back to cookie claims
            // if the backend /me endpoint rejects the token (e.g. PENDING account status).
            var meResult = await _authenticatedAuthApi.GetMeAsync(cancellationToken);
            CurrentUser? me = meResult.Value;
            if (me is null && httpContext.User.Identity is not null)
            {
                var roleStr = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
                UserRole? role = Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var parsed) ? parsed : null;
                me = new CurrentUser(
                    Id: httpContext.User.FindFirst(AuthClaimTypes.Sub)?.Value ?? "",
                    Email: httpContext.User.FindFirst(AuthClaimTypes.Email)?.Value ?? "",
                    DisplayName: httpContext.User.FindFirst(AuthClaimTypes.Name)?.Value ?? "",
                    Role: role,
                    EmailVerified: httpContext.User.FindFirst(AuthClaimTypes.EmailVerified)?.Value == "true",
                    OnboardingCompleted: httpContext.User.FindFirst(AuthClaimTypes.OnboardingCompleted)?.Value == "true",
                    OnboardingStatus: httpContext.User.FindFirst(AuthClaimTypes.OnboardingStatus)?.Value);
            }

            var isPersistent = httpContext.User.FindFirst(AuthClaimTypes.IsPersistent)?.Value == "true";
            var ttl = isPersistent ? TimeSpan.FromDays(30) : TimeSpan.FromHours(8);

            var principal = ClaimsFactory.CreatePrincipal(me!, sid, isPersistent);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(ttl),
                AllowRefresh = true
            };

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
            return Result<CurrentUser>.Success(me ?? throw new InvalidOperationException("CurrentUser could not be resolved."));
        }
        finally
        {
            SessionContext.SessionId = null;
        }
    }

    public async Task<Result> VerifyEmailAsync(
        string code,
        HttpContext? httpContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = httpContext ?? _httpContextAccessor.HttpContext;
        
        // Resolve SID from HttpContext or Blazor circuit's AuthenticationStateProvider
        var sid = context?.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(AuthClaimTypes.Sid)?.Value
            : await ResolveSessionIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(sid) && context?.User?.Identity?.IsAuthenticated != true)
        {
            return Result.Failure(new ApiError("UNAUTHORIZED", "User is not authenticated.", 401));
        }

        if (!string.IsNullOrEmpty(sid))
        {
            SessionContext.SessionId = sid;
        }

        try
        {
            var result = await _authenticatedAuthApi.VerifyEmailAsync(new VerifyEmailRequest(code), cancellationToken);
            if (result.IsFailure)
            {
                return result;
            }

            // Refresh the cookie claims to reflect the verified email status.
            // If /me fails (e.g. PENDING account), fall back to cookie claims with
            // emailVerified forced to true since verification just succeeded.
            var meResult = await _authenticatedAuthApi.GetMeAsync(cancellationToken);
            CurrentUser? me = meResult.Value;
            if (me is null && context?.User?.Identity is not null)
            {
                var roleStr = context.User.FindFirst(ClaimTypes.Role)?.Value;
                UserRole? role = Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var parsed) ? parsed : null;
                me = new CurrentUser(
                    Id: context.User.FindFirst(AuthClaimTypes.Sub)?.Value ?? "",
                    Email: context.User.FindFirst(AuthClaimTypes.Email)?.Value ?? "",
                    DisplayName: context.User.FindFirst(AuthClaimTypes.Name)?.Value ?? "",
                    Role: role,
                    EmailVerified: true,
                    OnboardingCompleted: context.User.FindFirst(AuthClaimTypes.OnboardingCompleted)?.Value == "true",
                    OnboardingStatus: context.User.FindFirst(AuthClaimTypes.OnboardingStatus)?.Value);
            }

            if (me is not null && sid is not null && context is not null)
            {
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
            }

            return result;
        }
        finally
        {
            SessionContext.SessionId = null;
        }
    }

    public async Task<Result<ResendVerificationResponse>> ResendVerificationAsync(
        CancellationToken cancellationToken = default)
    {
        // Resolve SID from HttpContext or Blazor circuit's AuthenticationStateProvider
        var sid = await ResolveSessionIdAsync(cancellationToken);
        if (!string.IsNullOrEmpty(sid))
        {
            SessionContext.SessionId = sid;
        }

        try
        {
            var result = await _authenticatedAuthApi.ResendVerificationAsync(cancellationToken);
            return result;
        }
        finally
        {
            SessionContext.SessionId = null;
        }
    }

    /// <summary>
    /// Fetches /api/auth/me directly with a bearer token, bypassing BearerTokenHandler.
    /// Used during SignInAsync where the cookie hasn't been set yet, so BearerTokenHandler
    /// cannot resolve the session from HttpContext.User.
    /// </summary>
    private async Task<CurrentUser?> FetchMeWithTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var handler = new HttpClientHandler();
            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(_apiOptions.BaseUrl),
                Timeout = TimeSpan.FromSeconds(10),
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync(AuthEndpoints.Me, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CurrentUser>(cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
