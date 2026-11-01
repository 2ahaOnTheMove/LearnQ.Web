using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace LearnQ.Presentation.Security;

public sealed record SessionTokenData(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

public interface IRedisSessionStore
{
    Task SetTokensAsync(string sid, SessionTokenData tokens, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<SessionTokenData?> GetTokensAsync(string sid, CancellationToken cancellationToken = default);
    Task RemoveTokensAsync(string sid, CancellationToken cancellationToken = default);
    Task InvalidateSessionAsync(string sid, CancellationToken cancellationToken = default);
}

public sealed class RedisSessionStore : IRedisSessionStore
{
    private const string KeyPrefix = "learnq-session-tokens:";
    private readonly IDistributedCache _cache;
    private readonly IDataProtector _protector;
    private readonly ITicketStore _ticketStore;

    public RedisSessionStore(IDistributedCache cache, IDataProtectionProvider dataProtectionProvider, ITicketStore ticketStore)
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector("LearnQ.SessionTokens.v1");
        _ticketStore = ticketStore;
    }

    public async Task SetTokensAsync(string sid, SessionTokenData tokens, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(tokens);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var protectedBytes = _protector.Protect(bytes);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        await _cache.SetAsync(KeyPrefix + sid, protectedBytes, options, cancellationToken);
    }

    public async Task<SessionTokenData?> GetTokensAsync(string sid, CancellationToken cancellationToken = default)
    {
        var protectedBytes = await _cache.GetAsync(KeyPrefix + sid, cancellationToken);
        if (protectedBytes is null || protectedBytes.Length == 0)
        {
            return null;
        }

        try
        {
            var bytes = _protector.Unprotect(protectedBytes);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<SessionTokenData>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task RemoveTokensAsync(string sid, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(KeyPrefix + sid, cancellationToken);
    }

    public async Task InvalidateSessionAsync(string sid, CancellationToken cancellationToken = default)
    {
        await RemoveTokensAsync(sid, cancellationToken);
        await _ticketStore.RemoveAsync(sid);
    }
}
