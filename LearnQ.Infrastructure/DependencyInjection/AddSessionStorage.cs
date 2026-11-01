using LearnQ.Application.Features.Auth.Session;
using LearnQ.Infrastructure.Configuration;
using LearnQ.Infrastructure.Sessions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace LearnQ.Infrastructure.DependencyInjection;

public static class SessionStorageExtensions
{
    public static IServiceCollection AddSessionStorage(this IServiceCollection services, IConfiguration config)
    {
        var redisConnectionString = config.GetConnectionString("Redis") ?? "localhost:6379";

        // Redis connection
        var redis = ConnectionMultiplexer.Connect(redisConnectionString);
        services.AddSingleton<IConnectionMultiplexer>(redis);

        // Distributed cache backed by Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = SessionStoreOptions.CacheInstanceName;
        });

        // Data Protection persisted in Redis
        services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, SessionStoreOptions.DataProtectionKeyCollection)
            .SetApplicationName("LearnQ");

        // Session token store (Spring access/refresh tokens in Redis)
        services.AddSingleton<ISessionTokenStore, RedisSessionTokenStore>();

        return services;
    }
}
