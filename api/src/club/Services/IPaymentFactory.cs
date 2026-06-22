namespace Club.Services;

public interface IPaymentFactory
{
    IPaymentProvider GetProvider(string providerName);
}
