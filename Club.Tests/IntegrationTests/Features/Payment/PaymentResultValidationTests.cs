using System.Net;
using IntegrationTests.Fixtures;

namespace IntegrationTests.Features.Payment;

[Collection("AppFixture collection")]
public class PaymentResultValidationTests(AppFixture app)
{
    [Fact]
    public async Task GetResult_WithoutTransactionId_RedirectsToFrontendFailurePage()
    {
        // Act - a bare browser hit on the return URL (no signed PayFast query params).
        // Use a client that does not auto-follow redirects so the Location header is inspectable.
        var redirectClient = app.CreateClient(new FastEndpoints.Testing.ClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        var response = await redirectClient.GetAsync("/payment/result/payfast", app.Context.CancellationToken);

        // Assert - the browser must never see raw JSON; bounce to the frontend failure page.
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldStartWith("http://localhost:5173/payment/failure?error=");
    }

    [Fact]
    public async Task PostResult_WithoutPayload_ReturnsInvalidWebhookJson()
    {
        // Act - the gateway's ITN/webhook contract stays JSON
        var response = await app.Client.PostAsync("/payment/result/payfast", content: null, app.Context.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync(app.Context.CancellationToken);
        body.ShouldContain("Invalid webhook request.");
    }
}
