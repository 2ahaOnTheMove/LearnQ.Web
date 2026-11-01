using System.Text.Json;
using LearnQ.Application.Features.Auth.Session;
using LearnQ.Infrastructure.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace LearnQ.Infrastructure.Sessions;

public sealed class RedisSessionTokenStore : ISessionTokenStore
{
    private readonly IDistributedCache _cache;
    private readonly IDataProtector _protector;

    public RedisSessionTokenStore(IDistributedCache cache, IDataProtectionProvider dataProtectionProvider)
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector(SessionStoreOptions.SessionTokensPurpose);
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

        await _cache.SetAsync(SessionStoreOptions.SessionTokenKeyPrefix + sid, protectedBytes, options, cancellationToken);
    }

    public async Task<SessionTokenData?> GetTokensAsync(string sid, CancellationToken cancellationToken = default)
    {
        var protectedBytes = await _cache.GetAsync(SessionStoreOptions.SessionTokenKeyPrefix + sid, cancellationToken);
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

    public async Task InvalidateSessionAsync(string sid, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(SessionStoreOptions.SessionTokenKeyPrefix + sid, cancellationToken);
    }
}
