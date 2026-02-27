using Online.Services;

namespace Online.Common.Extensions;

public static class GeneralExtensions
{
    public static IServiceCollection ConfigureGeneral(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<AuthTokenProcessor>();
        services.AddDetection();
        services.AddScoped<AccountService>();
        services.AddScoped<EncryptionService>();
        return services;
    }
}
