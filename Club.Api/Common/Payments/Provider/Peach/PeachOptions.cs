namespace Club.Common.Payments.Provider.Peach;

public class PeachOptions
{
    public const string Key = "PeachPayments";
    public required string EntityId { get; set; } = "8ac7a4c894809722019482d1df62029d";
    public required string UserId { get; set; } = "5fb5e392d6fa11ef9b3002f694e28f55";
    public required string Password { get; set; } = "OMydSc7ewVmEKPZCAj2WxHoik";
    public required string BaseUrl { get; set; } = "https://testapi-v2.peachpayments.com";
}
