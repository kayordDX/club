using StackExchange.Redis;

namespace Online.Common.Extensions;

public static class RedisExtensions
{
    public static IServiceCollection ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(o =>
        {
            o.Configuration = configuration.GetConnectionString("Redis");
        });
        return services;
    }
}