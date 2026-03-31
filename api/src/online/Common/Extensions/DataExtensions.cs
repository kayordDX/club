using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Online.Data;
using Online.Entities;
using TickerQ.EntityFrameworkCore.DbContextFactory;

namespace Online.Common.Extensions;

public static class DataExtensions
{
    public static IServiceCollection ConfigureEF(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        services.AddIdentity<User, IdentityRole<Guid>>(opt =>
        {
            opt.Password.RequireDigit = true;
            opt.Password.RequireLowercase = true;
            opt.Password.RequireNonAlphanumeric = true;
            opt.Password.RequireUppercase = true;
            opt.Password.RequiredLength = 8;
            opt.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders()
        .AddUserStore<UserStore>();

        services.AddScoped<UserStore>();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSnakeCaseNamingConvention();
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            );
            if (env.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
            }
        });

        services.Configure<IdentityOptions>(options =>
        {
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
        });
        return services;
    }

    public static async Task ApplyMigrations(this IServiceProvider serviceProvider, IWebHostEnvironment env, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tickerDb = scope.ServiceProvider.GetRequiredService<TickerQDbContext>();
        if (db.Database.IsNpgsql())
        {
            await db.Database.MigrateAsync(ct);
        }
        if (tickerDb.Database.IsNpgsql())
        {
            await tickerDb.Database.MigrateAsync(ct);
        }

        if (env.IsDevelopment() || env.IsEnvironment("Testing"))
        {
            await SeedDbContext.SeedData(db, ct);
        }
    }
}
