namespace Club.Common.Models;

using QueryKit.Configuration;

public class CustomQueryKitConfiguration(Action<QueryKitSettings>? configureSettings = null) : QueryKitConfiguration(settings =>
    {
        // configure custom global settings here
        // settings.EqualsOperator = "eq";
        configureSettings?.Invoke(settings);
    })
{
}
