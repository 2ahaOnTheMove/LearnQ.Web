using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LearnQ.Application.Common.Interfaces;
using LearnQ.Application.Features.Auth;
using LearnQ.Application.Features.Auth.Session;
using Microsoft.Extensions.DependencyInjection;

namespace LearnQ.Infrastructure.Api.Http;

public sealed class BearerTokenHandler : DelegatingHandler
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SessionLocks = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ISessionTokenStore _sessionStore;
    private readonly ISessionAccessor _sessionAccessor;

    public BearerTokenHandler(
        IServiceProvider serviceProvider,
        ISessionTokenStore sessionStore,
        ISessionAccessor sessionAccessor)
    {
        _serviceProvider = serviceProvider;
        _sessionStore = sessionStore;
        _sessionAccessor = sessionAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // AsyncLocal session ID set by AuthService bridges the DI scope gap where
        // IHttpClientFactory creates its own scope for DelegatingHandlers.
        var sid = SessionContext.SessionId
            ?? await _sessionAccessor.GetSessionIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(sid))
        {
            // No active session — return 401 locally instead of hitting the backend
            // without a token. This avoids unnecessary network calls and confusing
            // 401 logs from the backend when the caller is anonymous.
            return CreateSessionExpiredResponse(request);
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

        // 429 Too Many Requests is NOT an auth failure — pass through unchanged.
        // The RateLimitHandler (outer handler) will convert it to RateLimitedException.
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return response;
        }

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
