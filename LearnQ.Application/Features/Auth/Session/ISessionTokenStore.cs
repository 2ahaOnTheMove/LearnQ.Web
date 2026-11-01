namespace LearnQ.Application.Features.Auth.Session;

public sealed record SessionTokenData(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

public interface ISessionTokenStore
{
    Task SetTokensAsync(string sid, SessionTokenData tokens, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<SessionTokenData?> GetTokensAsync(string sid, CancellationToken cancellationToken = default);
    Task InvalidateSessionAsync(string sid, CancellationToken cancellationToken = default);
}
