using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Online.Common.Config;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Online.Common.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection ConfigureAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.JwtOptionsKey).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JwtOptions configuration is missing.");

        // Persist Data Protection keys so that auth cookies and OIDC nonce/correlation
        // cookies survive API restarts. Without persistence, every restart generates new
        // keys and all existing cookies become invalid ("Unprotect ticket failed"), which
        // breaks the OIDC callback flow and requires users to re-login.
        var dpKeysDir = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, ".dp-keys"));
        dpKeysDir.Create();
        services.AddDataProtection()
            .PersistKeysToFileSystem(dpKeysDir)
            .SetApplicationName("online");

        // AddIdentity() (called in ConfigureEF) sets DefaultAuthenticateScheme="Identity.Application"
        // and DefaultSignInScheme="Identity.External". We must explicitly override ALL scheme
        // defaults here so that:
        //   - Requests are authenticated against our ".AspNetCore.Cookies" session cookie.
        //   - The OpenIdConnect handler signs the user into the Cookie scheme (not Identity.External).
        //   - Unauthenticated requests are challenged via OpenIdConnect (OIDC redirect to IdP).
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Events = new CookieAuthenticationEvents
            {
                OnSignedIn = context =>
                {
                    // After OIDC sign-in, write a client-readable HAS_TOKEN cookie so the
                    // frontend can detect the session. The legacy JWT login flow sets this
                    // same cookie name, so no frontend changes are needed.
                    var expiresAtStr = context.Properties.GetTokenValue("expires_at");
                    DateTime tokenExpiry;
                    if (!string.IsNullOrEmpty(expiresAtStr) &&
                        DateTimeOffset.TryParse(expiresAtStr, null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var parsedExpiry))
                    {
                        tokenExpiry = parsedExpiry.UtcDateTime;
                    }
                    else
                    {
                        tokenExpiry = DateTime.UtcNow.AddMinutes(15);
                    }

                    context.Response.Cookies.Append("HAS_TOKEN", tokenExpiry.ToString("o"), new CookieOptions
                    {
                        HttpOnly = false,
                        Expires = context.Properties.ExpiresUtc?.UtcDateTime ?? DateTime.UtcNow.AddDays(1),
                        IsEssential = true,
                        Secure = false,
                        SameSite = SameSiteMode.Lax,
                    });
                    return Task.CompletedTask;
                }
            };
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = jwtOptions.Issuer;
            options.ClientId = "web_client";
            options.ResponseType = "code";
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.TokenValidationParameters.NameClaimType = "name";
            options.RequireHttpsMetadata = false;
            options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = context =>
                {
                    // Cross-origin requests are fetch/XHR calls from the SPA (e.g. localhost:5173
                    // calling localhost:5000). Redirecting them to the identity server causes a
                    // CORS failure because the browser follows the 302 cross-origin and the
                    // identity server does not add CORS headers on those endpoints.
                    // Return 401 so the SPA can handle the unauthenticated state gracefully.
                    var origin = context.Request.Headers.Origin.FirstOrDefault();
                    var currentOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
                    if (!string.IsNullOrEmpty(origin) && origin != currentOrigin)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.HandleResponse(); // suppress the redirect
                        return Task.CompletedTask;
                    }

                    context.ProtocolMessage.RedirectUri = "http://localhost:5000/signin-oidc";
                    return Task.CompletedTask;
                },

                OnRemoteFailure = context =>
                {
                    // Redirect back to the frontend with an error indicator instead of
                    // showing the default ASP.NET Core remote failure page.
                    context.Response.Redirect("http://localhost:5173?auth_error=1");
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
