using Microsoft.EntityFrameworkCore;
using Club.Data;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace Club.Common.Extensions;

public static class TickerExtensions
{
    public static void ConfigureTickerQ(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTickerQ(opt =>
        {
            opt.AddOperationalStore(o =>
            {
                o.UseTickerQDbContext<TickerQDbContext>(options =>
                {
                    options.UseSnakeCaseNamingConvention();
                    options.UseNpgsql(
                        configuration.GetConnectionString("DefaultConnection"),
                        b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                    );
                });
            });

            services.AddDbContext<TickerQDbContext>(options =>
            {
                options.UseSnakeCaseNamingConvention();
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                );
            });

            opt.AddDashboard(o =>
            {
                o.WithBasicAuth(configuration["TickerQBasicAuth:Username"]!, configuration["TickerQBasicAuth:Password"]!);
            });
        });
    }
}
