using Serilog;

namespace Online.Common.Extensions;

public static class LoggingExtensions
{
    public static void AddLoggingConfiguration(this IHostBuilder host, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
        host.UseSerilog();
    }
}