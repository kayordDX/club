using Aspire.Hosting.Pipelines;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume().WithHostPort(5432);

var clubDb = postgres.AddDatabase("club");

var cache = builder.AddRedis("cache");

var keycloak = builder
    .AddKeycloak("keycloak", 8088)
    .WithDataVolume()
    .WithBindMount("./keycloak/themes", "/opt/keycloak/providers")
    .WithRealmImport("./keycloak/realms");

// .WithEnvironment("KC_HTTP_ENABLED", "true")

var mailpit = builder
    .AddContainer("mailpit", "axllent/mailpit")
    .WithEnvironment("MP_MAX_MESSAGES", "5000")
    .WithEnvironment("MP_SMTP_AUTH_ACCEPT_ANY", "1")
    .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "1")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "http")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

// SvelteKit frontend — Vite dev mode, references API via service discovery
// Declared early so API can reference its endpoint for CORS
var web = builder.AddViteApp("web", "../client").WithPnpm().WithEnvironment("BROWSER", "none");

web.WithEndpoint(
    "http",
    e =>
    {
        e.Port = 5173;
        e.TargetPort = 5173;
        e.IsProxied = false;
    }
);
#pragma warning disable ASPIREBROWSERLOGS001
web = web.WithBrowserLogs();
#pragma warning restore ASPIREBROWSERLOGS001

// API — references Postgres and Redis
// Connection strings are aliased to match the API's expected config keys
var api = builder
    .AddProject("api", "../Club.Api/Club.Api.csproj")
    .WithReference(clubDb)
    .WithReference(cache)
    .WithReference(keycloak)
    .WithEnvironment("ConnectionStrings__DefaultConnection", clubDb)
    .WithEnvironment("ConnectionStrings__Redis", cache)
    .WaitFor(postgres)
    .WithEnvironment("Cors__0", web.GetEndpoint("http"))
    .WithEnvironment("Keycloak__AuthServerUrl", keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Issuer", $"{keycloak.GetEndpoint("http")}/realms/kayord")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

// Wire the frontend's reference to the API and Keycloak (after both are declared)
web = web.WithReference(api)
    .WaitFor(api)
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WithEnvironment("APP_URL", web.GetEndpoint("http"))
    .WithEnvironment("IDENTITY_URL", $"{keycloak.GetEndpoint("http")}/realms/kayord");

builder.Build().Run();
