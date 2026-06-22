using Club.Common.Payments;
using Club.Common.Payments.Provider.Peach;
using Club.Common.Payments.Provider.Payfast;
using Club.Services;

namespace Club.Common.Extensions;

public static class PaymentExtensions
{
    public static IServiceCollection ConfigurePayments(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PeachOptions>(configuration.GetSection(PeachOptions.Key));
        services.Configure<PayfastOptions>(configuration.GetSection(PayfastOptions.Key));

        services.AddHttpClient<PeachProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PeachOptions>>();
            client.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/'));
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddHttpClient<PayfastProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PayfastOptions>>();
            client.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/'));
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
        });

        services.AddScoped<IPaymentProvider, PeachProvider>();
        services.AddScoped<IPaymentProvider, PayfastProvider>();

        services.AddScoped<IPaymentFactory, PaymentFactory>();

        return services;
    }
}
