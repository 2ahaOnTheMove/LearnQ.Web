using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Features.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LearnQ.Presentation.Security;

/// <summary>
/// Attaches Spring JWT access token to outbound requests. Handles proactive refresh before expiry
/// and reactive refresh on 401 with request retry. Concurrency is guarded per-session via in-memory
/// locks (and backend rotating refresh token revocation on distributed instances).
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SessionLocks = new();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRedisSessionStore _sessionStore;

    public BearerTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        IRedisSessionStore sessionStore)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _sessionStore = sessionStore;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sid = await ResolveSidAsync();
        if (string.IsNullOrEmpty(sid))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var tokens = await _sessionStore.GetTokensAsync(sid, cancellationToken);
        if (tokens is null)
        {
            return CreateSessionExpiredResponse(request);
        }

        // Proactive token refresh if token expires within 60 seconds
        if (tokens.ExpiresAt <= DateTimeOffset.UtcNow.AddSeconds(60))
        {
            tokens = await RefreshSessionTokenAsync(sid, tokens, cancellationToken);
            if (tokens is null)
            {
                return CreateSessionExpiredResponse(request);
            }
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Buffer request content in memory so that on a 401 response the request can be retried
        byte[]? contentBytes = null;
        MediaTypeHeaderValue? contentType = null;
        if (request.Content is not null)
        {
            contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            contentType = request.Content.Headers.ContentType;
            request.Content = new ByteArrayContent(contentBytes);
            if (contentType is not null)
            {
                request.Content.Headers.ContentType = contentType;
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Reactive token refresh on 401 Unauthorized
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            tokens = await RefreshSessionTokenAsync(sid, tokens, cancellationToken);
            if (tokens is null)
            {
                response.Dispose();
                return CreateSessionExpiredResponse(request);
            }

            // Retry request with new token
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            if (contentBytes is not null)
            {
                request.Content = new ByteArrayContent(contentBytes);
                if (contentType is not null)
                {
                    request.Content.Headers.ContentType = contentType;
                }
            }

            response.Dispose();
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }

    private async Task<string?> ResolveSidAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var sid = httpContext.User.FindFirst(AppClaims.Sid)?.Value;
            if (!string.IsNullOrEmpty(sid))
            {
                return sid;
            }
        }

        // In interactive circuits HttpContext is null; resolve sid from AuthenticationStateProvider
        try
        {
            var authStateProvider = _serviceProvider.GetService<AuthenticationStateProvider>();
            if (authStateProvider is not null)
            {
                var state = await authStateProvider.GetAuthenticationStateAsync();
                return state.User.FindFirst(AppClaims.Sid)?.Value;
            }
        }
        catch
        {
            // Circuit may not have initialized state yet
        }

        return null;
    }

    private async Task<SessionTokenData?> RefreshSessionTokenAsync(string sid, SessionTokenData currentTokens, CancellationToken cancellationToken)
    {
        var sessionLock = SessionLocks.GetOrAdd(sid, _ => new SemaphoreSlim(1, 1));
        await sessionLock.WaitAsync(cancellationToken);
        try
        {
            // Check if another concurrent request already refreshed the tokens
            var latestTokens = await _sessionStore.GetTokensAsync(sid, cancellationToken);
            if (latestTokens is not null && latestTokens.AccessToken != currentTokens.AccessToken && latestTokens.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(60))
            {
                return latestTokens;
            }

            using var scope = _serviceProvider.CreateScope();
            var authApi = scope.ServiceProvider.GetRequiredService<IAuthApi>();
            var refreshResult = await authApi.RefreshTokenAsync(new RefreshTokenRequest(currentTokens.RefreshToken), cancellationToken);

            if (refreshResult.IsFailure || refreshResult.Value is null)
            {
                await _sessionStore.InvalidateSessionAsync(sid, cancellationToken);
                return null;
            }

            var refreshed = refreshResult.Value;
            var updatedTokens = new SessionTokenData(refreshed.AccessToken, refreshed.RefreshToken, refreshed.ExpiresAt);
            var ttl = refreshed.ExpiresAt - DateTimeOffset.UtcNow;
            if (ttl < TimeSpan.FromHours(1))
            {
                ttl = TimeSpan.FromHours(8);
            }

            await _sessionStore.SetTokensAsync(sid, updatedTokens, ttl, cancellationToken);
            return updatedTokens;
        }
        finally
        {
            sessionLock.Release();
        }
    }

    private static HttpResponseMessage CreateSessionExpiredResponse(HttpRequestMessage request)
    {
        var problem = new
        {
            type = "about:blank",
            title = "Unauthorized",
            status = 401,
            detail = "Your session has expired. Please sign in again.",
            code = "SESSION_EXPIRED"
        };

        return new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            RequestMessage = request,
            Content = new StringContent(JsonSerializer.Serialize(problem), Encoding.UTF8, "application/problem+json")
        };
    }
}
