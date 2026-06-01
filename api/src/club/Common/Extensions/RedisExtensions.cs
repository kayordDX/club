using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Hybrid;
using Online.Data;
using StackExchange.Redis;

namespace Online.Common.Extensions;

public static class RedisExtensions
{
    public static IServiceCollection ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is missing.");

        var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);

        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        services.AddStackExchangeRedisCache(o =>
        {
            o.ConnectionMultiplexerFactory = () =>
                Task.FromResult<IConnectionMultiplexer>(multiplexer);
        });

        services.AddDataProtection()
            .SetApplicationName("online")
            .PersistKeysToStackExchangeRedis(multiplexer, "DataProtection-Keys");

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromDays(30),
                LocalCacheExpiration = TimeSpan.FromMinutes(10)
            };
        });

        services.AddSingleton<ITicketStore, TicketStore>();

        return services;
    }
}
