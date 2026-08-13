using Club.Data;
using Club.Entities;
using FastEndpoints.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace IntegrationTests.Fixtures;

[CollectionDefinition("AppFixture collection")]
public class AppFixtureCollection : ICollectionFixture<AppFixture> { }

public class AppFixture : AppFixture<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _dbContainer;
    private RedisContainer? _redisContainer;
    private string _connectionString = string.Empty;
    private string _redisConnectionString = string.Empty;

    protected override async ValueTask PreSetupAsync()
    {
        // Start PostgreSQL TestContainer
        _dbContainer = new PostgreSqlBuilder("postgres:18")
            .WithImage("postgres:18")
            .WithDatabase("club_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _dbContainer.StartAsync();

        // Get connection string from the running container
        _connectionString = _dbContainer.GetConnectionString();

        // Start Redis TestContainer — the API connects to Redis eagerly at startup
        _redisContainer = new RedisBuilder().WithImage("redis:7-alpine").Build();
        await _redisContainer.StartAsync();
        _redisConnectionString = _redisContainer.GetConnectionString();
    }

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                // Load test configuration
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.Testing.json", optional: false);
                // Override the database and Redis connection strings with TestContainer connection strings
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = _connectionString,
                        ["ConnectionStrings:Redis"] = _redisConnectionString,
                    }
                );
            }
        );

        builder.UseEnvironment("Testing");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // We need to rebuild the DbContext with the test connection string
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSnakeCaseNamingConvention();
            options.UseNpgsql(_connectionString, b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            options.EnableSensitiveDataLogging();
        });

        // Disable Redis caching for tests - use memory cache instead
        services.AddMemoryCache();
        var redisCacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDistributedCache));
        if (redisCacheDescriptor != null)
        {
            services.Remove(redisCacheDescriptor);
        }

        // Authenticate all test requests as a fixed test user (JwtBearer can't reach Keycloak in tests)
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
    }

    protected override async ValueTask SetupAsync()
    {
        // Apply migrations on the test database
        await using var scope = Server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database is created and migrations are applied
        await db.Database.MigrateAsync();

        // Seed the user that authenticated requests resolve to (booking.user_id has an FK to the users table)
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        if (await userManager.FindByIdAsync(TestClaims.UserId) is null)
        {
            await userManager.CreateAsync(
                new User
                {
                    Id = TestClaims.UserIdGuid,
                    UserName = "test-user",
                    Email = "test@example.com",
                    EmailConfirmed = true,
                    FirstName = "Test",
                    LastName = "User",
                }
            );
        }
    }

    protected override async ValueTask TearDownAsync()
    {
        // Database will be cleaned up automatically when container stops
        if (_dbContainer != null)
        {
            await _dbContainer.StopAsync();
            await _dbContainer.DisposeAsync();
        }

        if (_redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }
    }
}

// Configuration class to match appsettings structure
public class ConnectionStrings
{
    public string DefaultConnection { get; set; } = string.Empty;
}
