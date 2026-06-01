using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Online.Common.Extensions;

public static class CorsExtensions
{
    private static readonly string _allowedOrigins = "OnlineOrigins";

    public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSection = configuration.GetSection("Cors");
        string[] origins = corsSection.Get<string[]>() ?? [""];
        services.AddCors(delegate (CorsOptions options)
        {
            options.AddPolicy(_allowedOrigins, delegate (CorsPolicyBuilder builder)
            {
                builder.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public static IApplicationBuilder UseCorsKayord(this IApplicationBuilder app)
    {
        app.UseCors(_allowedOrigins);
        return app;
    }
}