using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Club.Common.Auth;
using Club.Common.Config;
using Keycloak.AuthServices.Common;
using Duende.AccessTokenManagement;
using Club.Services;

namespace Club.Common.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection ConfigureAuth(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        var keycloakConfig = configuration.GetSection(KeycloakConfig.Key).Get<KeycloakConfig>()
            ?? throw new InvalidOperationException("Keycloak configuration is missing.");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = keycloakConfig.Issuer;
            options.Audience = keycloakConfig.Audience;
            options.RequireHttpsMetadata = !env.IsDevelopment();
            options.MetadataAddress = keycloakConfig.MetadataAddress;
            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
            };
        });

        Keycloak.AuthServices.Sdk.KeycloakAdminClientOptions options = new()
        {
            Realm = keycloakConfig.Realm,
            Resource = keycloakConfig.AdminClientId,
            AuthServerUrl = keycloakConfig.AuthServerUrl,
            Credentials = new KeycloakClientInstallationCredentials
            {
                Secret = keycloakConfig.AdminClientSecret
            },
        };

        Keycloak.AuthServices.Sdk.Kiota.KeycloakAdminClientOptions kiotaOptions = new()
        {
            Realm = keycloakConfig.Realm,
            Resource = keycloakConfig.AdminClientId,
            AuthServerUrl = keycloakConfig.AuthServerUrl,
            Credentials = new KeycloakClientInstallationCredentials
            {
                Secret = keycloakConfig.AdminClientSecret
            },
        };

        var tokenClientName = ClientCredentialsClientName.Parse(keycloakConfig.AdminClientId);

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
            .AddKeycloakAdminHttpClient(services, kiotaOptions)
            .AddClientCredentialsTokenHandler(tokenClientName);

        services.AddScoped<ICurrentFacilityAccessor, CurrentFacilityAccessor>();
        services.AddScoped<IAuthorizationHandler, FacilityRoleHandler>();

        services.AddAuthorization(o =>
        {
            o.AddFacilityRolePolicy(global::Club.Constants.Policy.Manager);
        });

        services.AddScoped<ICustomKeycloakService, CustomKeycloakService>();

        return services;
    }
}
