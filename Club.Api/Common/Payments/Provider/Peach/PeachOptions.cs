namespace Club.Common.Payments.Provider.Peach;

public class PeachOptions
{
    public const string Key = "PeachPayments";
    public required string EntityId { get; set; }
    public required string UserId { get; set; }
    public required string Password { get; set; }
    public required string BaseUrl { get; set; }
}
