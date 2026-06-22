namespace Club.Common.Payments.Provider.Payfast;

public class PayfastOptions
{
    public const string Key = "Payfast";
    public required string MerchantId { get; set; }
    public required string MerchantKey { get; set; }
    public required string Passphrase { get; set; }
    public required string BaseUrl { get; set; }
    public required string ReturnUrl { get; set; }
    public required string CancelUrl { get; set; }
    public required string NotifyUrl { get; set; }
}
