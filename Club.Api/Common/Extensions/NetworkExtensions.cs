using Microsoft.AspNetCore.HttpOverrides;
using Club.Common.Config;

namespace Club.Common.Extensions;

public static class NetworkExtensions
{
    public static IServiceCollection ConfigureNetwork(this IServiceCollection services, IConfiguration configuration)
    {
        var appConfig = configuration.GetSection("App").Get<AppConfig>() ?? throw new ArgumentNullException(nameof(configuration), "App configuration is missing.");

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;

            // Trust only Docker's internal bridge network where Traefik runs.
            // ForwardLimit = 1 ensures only the header set by Traefik itself is trusted,
            // docker network inspect kayord_default | grep Subnet
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            // options.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.Parse(appConfig.DockerSubnet), 12));
            options.ForwardLimit = 1;
        });

        return services;
    }
}
