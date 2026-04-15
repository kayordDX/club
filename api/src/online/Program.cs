using Microsoft.AspNetCore.HttpOverrides;
using Online.Common.Extensions;
using TickerQ.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// OIDC nonce/correlation cookies accumulate (path=/signin-oidc) when the auth flow fails
// repeatedly. Raise the Kestrel header limit so those accumulated cookies don't cause HTTP 431
// before the flow can succeed and clean them up.
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Limits.MaxRequestHeadersTotalSize = 128 * 1024; // 128 KB (default is 32 KB)
});

builder.Services.ConfigureApi();
builder.Services.ConfigureConfig(builder.Configuration);
builder.Services.ConfigureRedis(builder.Configuration);

builder.Services.ConfigureHealth(builder.Configuration);
builder.Services.ConfigureCors(builder.Configuration);

builder.Services.ConfigureEF(builder.Configuration, builder.Environment);
builder.Services.ConfigureTickerQ(builder.Configuration);
builder.Services.ConfigureGeneral(builder.Configuration);
builder.Services.ConfigureAuth(builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Logging.ConfigureLogging();
builder.Services.ConfigureTelemetry(builder.Configuration);

var app = builder.Build();

await app.Services.ApplyMigrations(app.Environment, app.Lifetime.ApplicationStopping);

app.UseForwardedHeaders();
app.UseCorsKayord();
app.UseAuthentication();
app.UseAuthorization();

app.UseDetection();
app.UseApi();
app.UseHealth();
app.UseTickerQ();
app.Run();
