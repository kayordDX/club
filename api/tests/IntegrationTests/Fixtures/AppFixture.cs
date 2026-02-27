using FastEndpoints.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Online.Data;
using Testcontainers.PostgreSql;

namespace IntegrationTests.Fixtures;

[CollectionDefinition("AppFixture collection")]
public class AppFixtureCollection : ICollectionFixture<AppFixture>
{
}

public class AppFixture : AppFixture<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _dbContainer;
    private string _connectionString = string.Empty;

    protected override async ValueTask PreSetupAsync()
    {
        // Start PostgreSQL TestContainer
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .WithDatabase("online_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _dbContainer.StartAsync();

        // Get connection string from the running container
        _connectionString = _dbContainer.GetConnectionString();
    }

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Load test configuration
            config.SetBasePath(Directory.GetCurrentDirectory());
            config.AddJsonFile("appsettings.Testing.json", optional: false);
        });

        builder.UseEnvironment("Testing");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // Override the database connection string with TestContainer connection string
        services.Configure<ConnectionStrings>(opts =>
        {
            opts.DefaultConnection = _connectionString;
        });

        // We need to rebuild the DbContext with the test connection string
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSnakeCaseNamingConvention();
            options.UseNpgsql(
                _connectionString,
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            );
            options.EnableSensitiveDataLogging();
        });

        // Disable Redis caching for tests - use memory cache instead
        services.AddMemoryCache();
        var redisCacheDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IDistributedCache));
        if (redisCacheDescriptor != null)
        {
            services.Remove(redisCacheDescriptor);
        }
    }

    protected override async ValueTask SetupAsync()
    {
        // Apply migrations on the test database
        await using var scope = Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database is created and migrations are applied
        await db.Database.MigrateAsync();
    }

    protected override async ValueTask TearDownAsync()
    {
        // Database will be cleaned up automatically when container stops
        if (_dbContainer != null)
        {
            await _dbContainer.StopAsync();
            await _dbContainer.DisposeAsync();
        }
    }
}

// Configuration class to match appsettings structure
public class ConnectionStrings
{
    public string DefaultConnection { get; set; } = string.Empty;
}
