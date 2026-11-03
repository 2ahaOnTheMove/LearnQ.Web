using System.Runtime.Serialization;

namespace LearnQ.Application.Common.Exceptions;

/// <summary>
/// Thrown when a 429 Too Many Requests response is received from the API.
/// This exception should NOT trigger token refresh, sign-out, or redirect to login.
/// </summary>
public class RateLimitedException : Exception
{
    public RateLimitedException(string scope, int retryAfterSeconds)
        : base($"Rate limited ({scope}). Retry after {retryAfterSeconds} seconds.")
    {
        Scope = scope;
        RetryAfterSeconds = retryAfterSeconds;
    }

    /// <summary>
    /// The rate limit scope (e.g., "auth.login", "auth.verify").
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// Seconds to wait before retrying.
    /// </summary>
    public int RetryAfterSeconds { get; }
}
