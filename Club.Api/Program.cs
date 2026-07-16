using Club.Common.Extensions;
using TickerQ.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.ConfigureApi(builder.Configuration);
builder.Services.ConfigureConfig(builder.Configuration);
builder.Services.ConfigureRedis(builder.Configuration);

builder.Services.ConfigureHealth(builder.Configuration);
builder.Services.ConfigureCors(builder.Configuration);

builder.Services.ConfigureEF(builder.Configuration, builder.Environment);
builder.Services.ConfigureTickerQ(builder.Configuration);
builder.Services.ConfigureGeneral(builder.Configuration);
builder.Services.ConfigurePayments(builder.Configuration);
builder.Services.ConfigureAuth(builder.Configuration, builder.Environment);
builder.Services.ConfigureAWS(builder.Configuration);
builder.Services.ConfigureNetwork(builder.Configuration);

// Add Npgsql and EF Core instrumentation on top of AddServiceDefaults()
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddNpgsqlInstrumentation())
    .WithTracing(tracing => tracing
        .AddEntityFrameworkCoreInstrumentation()
        .AddNpgsql());

var app = builder.Build();

await app.Services.ApplyMigrations(app.Environment, app.Lifetime.ApplicationStopping);

app.UseForwardedHeaders();
app.UseCorsKayord();
app.UseAuthentication();
app.UseAuthorization();

app.UseApi();
app.MapDefaultEndpoints();
app.UseTickerQ();
app.Run();
