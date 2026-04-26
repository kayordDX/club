using FastEndpoints.Swagger;
using NSwag;
using Scalar.AspNetCore;

namespace Online.Common.Extensions;

public static class ApiExtensions
{
    public static void ConfigureApi(this IServiceCollection services)
    {
        services.AddResponseCompression(o =>
        {
            o.EnableForHttps = true;
        });
        services.AddFastEndpoints();

        services.SwaggerDocument(o =>
        {
            o.ShortSchemaNames = true;
            o.RemoveEmptyRequestSchema = true;
            o.EnableJWTBearerAuth = false;
            o.DocumentSettings = s =>
            {
                s.Title = AppDomain.CurrentDomain.FriendlyName;
                s.Version = "v1";
                s.MarkNonNullablePropsAsRequired();
                s.AddAuth("Keycloak", new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = "http://localhost:18080/realms/kayord/protocol/openid-connect/auth",
                            Scopes = new Dictionary<string, string>
                            {
                                { "openid", "OpenID Connect scope" },
                                { "profile", "Profile scope" },
                                { "email", "Email scope" }
                            }
                        },
                    }
                });
            };
        });
    }

    public static IApplicationBuilder UseApi(this WebApplication app)
    {
        app.UseResponseCompression();
        app.UseDefaultExceptionHandler()
            .UseFastEndpoints(c =>
            {
                c.Serializer.Options.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                c.Endpoints.Configurator = ep =>
                {
                    ep.Options(x => x.Produces<InternalErrorResponse>(500));
                };
            });

        // Only Have docs available in development
        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
            app.MapScalarApiReference(options =>
            {
                options.TagSorter = TagSorter.Alpha;
            });
        }
        return app;
    }
}
