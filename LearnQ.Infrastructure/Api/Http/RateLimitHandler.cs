using System.Net;
using System.Text.Json;
using LearnQ.Application.Common.Exceptions;

namespace LearnQ.Infrastructure.Api.Http;

/// <summary>
/// DelegatingHandler that intercepts 429 responses and throws RateLimitedException.
/// Does NOT retry automatically, does NOT trigger token refresh, does NOT sign out.
/// Only idempotent GETs may auto-retry ONCE when Retry-After is 2s or less.
/// </summary>
public sealed class RateLimitHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            // Read Retry-After header
            int retryAfter = 30;
            if (response.Headers.RetryAfter?.Delta.HasValue == true)
            {
                retryAfter = (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;
            }
            else if (response.Headers.RetryAfter is not null)
            {
                // Retry-After can be a date string; try to parse it
                if (DateTimeOffset.TryParse(response.Headers.RetryAfter.ToString(), out var retryDate))
                {
                    retryAfter = (int)(retryDate - DateTimeOffset.UtcNow).TotalSeconds;
                }
            }

            // Try to read scope from body
            string? scope = null;
            if (response.Content is not null)
            {
                try
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    var doc = JsonDocument.Parse(body);
                    scope = doc.RootElement.TryGetProperty("scope", out var s) ? s.GetString() : null;
                    if (string.IsNullOrEmpty(scope))
                    {
                        scope = doc.RootElement.TryGetProperty("code", out var c) ? c.GetString() : null;
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
            }

            scope ??= "unknown";
            retryAfter = Math.Max(1, Math.Min(retryAfter, 3600));

            // For idempotent GETs with very short retry-after, auto-retry once
            if (request.Method == HttpMethod.Get && retryAfter <= 2)
            {
                response.Dispose();
                await Task.Delay(retryAfter * 1000 + Random.Shared.Next(0, 500), cancellationToken);
                return await base.SendAsync(request, cancellationToken);
            }

            // Throw RateLimitedException — caller handles it
            response.Dispose();
            throw new RateLimitedException(scope, retryAfter);
        }

        return response;
    }
}
