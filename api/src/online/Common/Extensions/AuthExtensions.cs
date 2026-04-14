using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Online.Common.Config;
using OpenIddict.Validation.AspNetCore;

namespace Online.Common.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection ConfigureAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.JwtOptionsKey).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JwtOptions configuration is missing.");

        services.AddOpenIddict()
            .AddValidation(options =>
            {

                var corsSection = configuration.GetSection("App");
                options.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.EncryptionKey)));
                var pfx = X509CertificateLoader.LoadPkcs12FromFile(jwtOptions.SigningCertPath, jwtOptions.SigningCertPassword);
                options.AddSigningCertificate(pfx);

                // var signinKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));
                // options.AddEncryptionKey(signinKey);
                // options.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String("eGztWK7NMvHAqUZrczQgLKjH8oFSg0ovnknlfahXxGg=")));
                // options.AddEphemeralEncryptionKey()
                //        .AddEphemeralSigningKey();

                // options.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.EncryptionKey)));
                // options.AddSigningKey(new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.SigningKey)));
                // Use the OpenIddict server as the token authority.
                // The validation handler will use the discovery endpoint at
                // {Issuer}/.well-known/openid-configuration to fetch the JWKS
                // signing keys and validate bearer tokens locally.
                options.SetIssuer(jwtOptions.Issuer);

                // Allow the validation handler to call the issuer over HTTP
                // to retrieve the discovery document and JWKS.
                options.UseSystemNetHttp();

                // Register the ASP.NET Core host so bearer tokens in the
                // Authorization header are picked up automatically.
                options.UseAspNetCore();
            });

        services.AddAuthentication(o =>
        {
            // Override the schemes Identity registered so bearer-token endpoints
            // get a proper 401/403 instead of a cookie redirect to /Account/Login.
            // DefaultSignInScheme is intentionally left alone so Identity's
            // SignInManager can still create cookies for account flows.
            o.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            o.DefaultForbidScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });
        services.AddAuthorization();

        return services;
    }
}
