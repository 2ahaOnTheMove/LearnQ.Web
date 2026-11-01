using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace LearnQ.Presentation.Security;

public sealed class RedisTicketStore : ITicketStore
{
    private const string KeyPrefix = "learnq-auth-ticket:";
    private readonly IDistributedCache _cache;
    private readonly IDataProtector _protector;

    public RedisTicketStore(IDistributedCache cache, IDataProtectionProvider dataProtectionProvider)
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector("LearnQ.AuthTickets.v1");
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var sid = ticket.Principal.FindFirst(AppClaims.Sid)?.Value
                  ?? Guid.NewGuid().ToString("N");

        var key = KeyPrefix + sid;
        var ticketBytes = TicketSerializer.Default.Serialize(ticket);
        var protectedBytes = _protector.Protect(ticketBytes);

        var options = CreateCacheOptions(ticket);
        await _cache.SetAsync(key, protectedBytes, options);

        return sid;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var cacheKey = KeyPrefix + key;
        var ticketBytes = TicketSerializer.Default.Serialize(ticket);
        var protectedBytes = _protector.Protect(ticketBytes);

        var options = CreateCacheOptions(ticket);
        await _cache.SetAsync(cacheKey, protectedBytes, options);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var cacheKey = KeyPrefix + key;
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
        var cacheKey = KeyPrefix + key;
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
