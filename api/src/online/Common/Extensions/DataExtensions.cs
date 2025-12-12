using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Online.Data;
using Online.Entities;

namespace Online.Common.Extensions;

public static class DataExtensions
{
    public static IServiceCollection ConfigureEF(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddIdentity<User, IdentityRole<Guid>>(opt =>
        {
            opt.Password.RequireDigit = true;
            opt.Password.RequireLowercase = true;
            opt.Password.RequireNonAlphanumeric = true;
            opt.Password.RequireUppercase = true;
            opt.Password.RequiredLength = 8;
            opt.User.RequireUniqueEmail = true;
        }).AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSnakeCaseNamingConvention();
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            );
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
            }
        });

        services.Configure<IdentityOptions>(options =>
        {
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
        });
        return services;
    }

    public static async Task ApplyMigrations(this IServiceProvider serviceProvider, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsNpgsql())
        {
            await db.Database.MigrateAsync(ct);
        }
    }
}