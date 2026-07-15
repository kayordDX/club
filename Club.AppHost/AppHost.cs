var builder = DistributedApplication.CreateBuilder(args);

// Postgres with app database (Aspire manages credentials)
var pg = builder.AddPostgres("pg");
var clubDb = pg.AddDatabase("club");

// Redis cache (Aspire manages TLS + credentials)
var cache = builder.AddRedis("cache");

// Mailpit — email testing SMTP + web UI
var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithEnvironment("MP_MAX_MESSAGES", "5000")
    .WithEnvironment("MP_SMTP_AUTH_ACCEPT_ANY", "1")
    .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "1")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "http")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

// SvelteKit frontend — Vite dev mode, references API via service discovery
// Declared early so API can reference its endpoint for CORS
var web = builder.AddViteApp("web", "../client")
    .WithPnpm()
    .WithEnvironment("BROWSER", "none");
#pragma warning disable ASPIREBROWSERLOGS001
web = web.WithBrowserLogs();
#pragma warning restore ASPIREBROWSERLOGS001

// API — references Postgres and Redis
// Connection strings are aliased to match the API's expected config keys
var api = builder.AddProject("api", "../Club.Api/club.csproj")
    .WithReference(clubDb)
    .WithReference(cache)
    .WithEnvironment("ConnectionStrings__DefaultConnection", clubDb)
    .WithEnvironment("ConnectionStrings__Redis", cache)
    .WaitFor(pg)
    .WithEnvironment("Cors__0", web.GetEndpoint("http"))
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

// Wire the frontend's reference to the API (after both are declared)
web = web.WithReference(api)
    .WaitFor(api)
    .WithEnvironment("PUBLIC_API_URL", api.GetEndpoint("http"));

builder.Build().Run();
