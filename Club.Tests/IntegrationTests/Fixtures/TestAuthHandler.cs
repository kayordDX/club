using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegrationTests.Fixtures;

public static class TestClaims
{
    public const string UserId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    public static readonly Guid UserIdGuid = Guid.Parse(UserId);
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey("X-Test-Anonymous"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId =
            Request.Headers.TryGetValue("X-Test-User-Id", out var values) && Guid.TryParse(values.ToString(), out var parsedUserId)
                ? parsedUserId
                : TestClaims.UserIdGuid;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userId == TestClaims.UserIdGuid ? "Test User" : "Test Customer"),
            new Claim(ClaimTypes.Email, userId == TestClaims.UserIdGuid ? "test@example.com" : $"{userId}@example.com"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
