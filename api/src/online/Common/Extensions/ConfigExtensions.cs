using Online.Common.Config;

namespace Online.Common.Extensions;

public static class ConfigExtensions
{
    public static IServiceCollection ConfigureConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.JwtOptionsKey));
        return services;
    }
}