namespace Club.Entities;

public class Wallet
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public int IsActive { get; set; }
    public string Currency { get; set; } = "ZAR";
}
