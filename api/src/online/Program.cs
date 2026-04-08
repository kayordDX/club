using Microsoft.AspNetCore.HttpOverrides;
using Online.Common.Extensions;
using TickerQ.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

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

    options.KnownNetworks.Clear();
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
