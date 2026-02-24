using FastEndpoints.Swagger;
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
                // s.OperationProcessors.Add(new CustomOperationsProcessor());
                // s.SchemaSettings.SchemaNameGenerator = new CustomSchemaNameGenerator(false);
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
