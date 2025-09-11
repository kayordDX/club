using Online.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddLoggingConfiguration(builder.Configuration);
builder.Services.ConfigureApi();
builder.Services.ConfigureRedis(builder.Configuration);

builder.Services.ConfigureHealth(builder.Configuration);
builder.Services.ConfigureCors(builder.Configuration);

builder.Services.ConfigureAuth(builder.Configuration);
builder.Services.ConfigureEF(builder.Configuration, builder.Environment);
builder.Services.ConfigureGeneral(builder.Configuration);

var app = builder.Build();

app.UseCorsKayord();
app.UseApi();
app.UseHealth();
app.UseAuthentication();
app.UseAuthorization();
app.Run();