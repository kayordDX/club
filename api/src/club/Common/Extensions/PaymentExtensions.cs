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

        services.AddScoped(typeof(IPaymentOptionsAccessor<>), typeof(PaymentOptionsAccessor<>));

        services.AddHttpClient<PeachProvider>();
        services.AddHttpClient<PayfastProvider>();

        services.AddScoped<IPaymentProvider, PeachProvider>();
        services.AddScoped<IPaymentProvider, PayfastProvider>();

        services.AddScoped<IPaymentFactory, PaymentFactory>();

        return services;
    }
}
