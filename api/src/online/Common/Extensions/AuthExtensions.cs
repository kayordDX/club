using Microsoft.AspNetCore.Authentication.JwtBearer;
using Online.Common.Config;
using Keycloak.AuthServices.Common;
using Duende.AccessTokenManagement;
using Keycloak.AuthServices.Sdk;

namespace Online.Common.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection ConfigureAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.JwtOptionsKey).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JwtOptions configuration is missing.");

        // services.AddScoped<TokenRefreshService>();

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = jwtOptions.Issuer;
            options.Audience = jwtOptions.Audience;
            options.RequireHttpsMetadata = false;
            options.MetadataAddress = "http://localhost:18080/realms/kayord/.well-known/openid-configuration";
        });

        var options = configuration.GetKeycloakOptions<KeycloakAdminClientOptions>()!;
        // options.AuthServerUrl = "http://localhost:18080/realms/kayord";

        var tokenClientName = ClientCredentialsClientName.Parse("admin-client");

        services.AddDistributedMemoryCache();
        services
            .AddClientCredentialsTokenManagement()
            .AddClient(
                tokenClientName,
                client =>
                {
                    client.ClientId = ClientId.Parse(options.Resource);
                    client.ClientSecret = ClientSecret.Parse(options.Credentials.Secret);
                    client.TokenEndpoint = new Uri(options.KeycloakTokenEndpoint);
                }
            );

        Keycloak.AuthServices.Sdk.ServiceCollectionExtensions
            .AddKeycloakAdminHttpClient(services, options)
            .AddClientCredentialsTokenHandler(tokenClientName);

        Keycloak.AuthServices.Sdk.Kiota.ServiceCollectionExtensions
            .AddKiotaKeycloakAdminHttpClient(services, configuration)
            .AddClientCredentialsTokenHandler(tokenClientName);


        // services.AddClientCredentialsTokenHandler(tokenClientName);

        // var keycloakOptions = new KeycloakAdminClientOptions
        // {
        //     AuthServerUrl = "http://localhost:18080/realms/kayord",
        //     Realm = "kayord",
        //     Resource = "admin-client",
        // };
        // services.AddKeycloakAdminHttpClient(keycloakOptions);

        services.AddAuthorization();
        return services;
    }
}
