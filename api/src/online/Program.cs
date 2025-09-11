using Online.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureApi();
builder.Services.ConfigureConfig(builder.Configuration);
builder.Services.ConfigureRedis(builder.Configuration);

builder.Services.ConfigureHealth(builder.Configuration);
builder.Services.ConfigureCors(builder.Configuration);

builder.Services.ConfigureEF(builder.Configuration, builder.Environment);
builder.Services.ConfigureAuth(builder.Configuration);
builder.Services.ConfigureGeneral(builder.Configuration);

builder.Logging.ConfigureLogging();
builder.Services.ConfigureTelemetry(builder.Configuration);

var app = builder.Build();

app.Services.ApplyMigrations();

app.UseCorsKayord();
app.UseApi();
app.UseHealth();
app.Run();