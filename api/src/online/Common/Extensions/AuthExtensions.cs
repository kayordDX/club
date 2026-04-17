using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Online.Common.Config;
using Online.Data;
using OpenIddict.Client;
using System.Security.Cryptography.X509Certificates;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Online.Common.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection ConfigureAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.JwtOptionsKey).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JwtOptions configuration is missing.");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = jwtOptions.Issuer;
            options.Audience = jwtOptions.Audience;
            options.RequireHttpsMetadata = false;
        });

        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<ITicketStore>((options, ticketStore) =>
            {
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.SessionStore = ticketStore;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            });

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore().UseDbContext<AppDbContext>();
            })
            .AddClient(options =>
            {
                options.AllowAuthorizationCodeFlow();
                options.UseSystemNetHttp();

                options.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.EncryptionKey)));
                var pfx = X509CertificateLoader.LoadPkcs12FromFile(jwtOptions.SigningCertPath, jwtOptions.SigningCertPassword);
                options.AddSigningCertificate(pfx);

                options.UseAspNetCore()
                       .EnableRedirectionEndpointPassthrough()
                       .DisableTransportSecurityRequirement();

                options.AddRegistration(new OpenIddictClientRegistration
                {
                    Issuer = new Uri(jwtOptions.Issuer),
                    ClientId = "web_client",
                    ResponseModes = { ResponseModes.Query },
                    ResponseTypes = { ResponseTypes.Code },
                    Scopes = { Scopes.Email, Scopes.Profile, Scopes.OfflineAccess },
                    RedirectUri = new Uri("http://localhost:5000/auth/login/callback")
                });
            });

        services.AddAuthorization(ConfigureAuthorization);
        return services;
    }

    static void ConfigureAuthorization(AuthorizationOptions options)
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build();
    }
}
