using System.Net;
using Club.Common.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UnitTests.Health;

public class HealthEndpointTests
{
    [Theory]
    [InlineData(true, HttpStatusCode.OK)]
    [InlineData(false, HttpStatusCode.ServiceUnavailable)]
    public async Task ProductionHealthEndpointReflectsReadinessWithoutAuthentication(bool healthy, HttpStatusCode expectedStatus)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.Services.AddHealthChecks().AddCheck("dependency", () => healthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());

        await using var app = builder.Build();
        app.UseHealth();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var response = await client.GetAsync("/health");

        Assert.Equal(expectedStatus, response.StatusCode);
        await app.StopAsync();
    }
}
