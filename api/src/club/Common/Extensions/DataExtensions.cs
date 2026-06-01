using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Club.Data;
using Club.Entities;
using TickerQ.EntityFrameworkCore.DbContextFactory;

namespace Club.Common.Extensions;

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
            opt.User.RequireUniqueEmail = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders()
        .AddUserStore<UserStore>();

        // Disable automatic redirects for API; return 403 instead of redirecting to /Account/Login
        services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

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

        // Bypassing for now and always seeding during testing
        if (env.IsDevelopment() || env.IsEnvironment("Testing") || true)
        {
            await SeedDbContext.SeedData(db, ct);
        }
    }
}
