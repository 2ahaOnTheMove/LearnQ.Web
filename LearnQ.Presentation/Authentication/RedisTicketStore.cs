using LearnQ.Application.Common.Constants;
using LearnQ.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace LearnQ.Presentation.Authentication;

public sealed class RedisTicketStore : ITicketStore
{
    private readonly IDistributedCache _cache;
    private readonly IDataProtector _protector;

    public RedisTicketStore(IDistributedCache cache, IDataProtectionProvider dataProtectionProvider)
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector(SessionStoreOptions.AuthTicketsPurpose);
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var sid = ticket.Principal.FindFirst(AuthClaimTypes.Sid)?.Value
                  ?? Guid.NewGuid().ToString("N");

        var key = SessionStoreOptions.AuthTicketKeyPrefix + sid;
        var ticketBytes = TicketSerializer.Default.Serialize(ticket);
        var protectedBytes = _protector.Protect(ticketBytes);

        var options = CreateCacheOptions(ticket);
        await _cache.SetAsync(key, protectedBytes, options);

        return sid;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var cacheKey = SessionStoreOptions.AuthTicketKeyPrefix + key;
        var ticketBytes = TicketSerializer.Default.Serialize(ticket);
        var protectedBytes = _protector.Protect(ticketBytes);

        var options = CreateCacheOptions(ticket);
        await _cache.SetAsync(cacheKey, protectedBytes, options);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var cacheKey = SessionStoreOptions.AuthTicketKeyPrefix + key;
        var protectedBytes = await _cache.GetAsync(cacheKey);
        if (protectedBytes is null || protectedBytes.Length == 0)
        {
            return null;
        }

        try
        {
            var ticketBytes = _protector.Unprotect(protectedBytes);
            return TicketSerializer.Default.Deserialize(ticketBytes);
        }
        catch
        {
            return null;
        }
    }

    public async Task RemoveAsync(string key)
    {
        var cacheKey = SessionStoreOptions.AuthTicketKeyPrefix + key;
        await _cache.RemoveAsync(cacheKey);
    }

    private static DistributedCacheEntryOptions CreateCacheOptions(AuthenticationTicket ticket)
    {
        var options = new DistributedCacheEntryOptions();
        if (ticket.Properties.ExpiresUtc.HasValue)
        {
            options.SetAbsoluteExpiration(ticket.Properties.ExpiresUtc.Value);
        }
        else
        {
            options.SetSlidingExpiration(TimeSpan.FromHours(8));
        }

        return options;
    }
}
