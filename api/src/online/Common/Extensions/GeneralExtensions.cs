using Online.Services;

namespace Online.Common.Extensions;

public static class GeneralExtensions
{
    public static IServiceCollection ConfigureGeneral(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AuthTokenProcessor>();
        services.AddScoped<UserRepository>();
        services.AddScoped<AccountService>();
        return services;
    }
}