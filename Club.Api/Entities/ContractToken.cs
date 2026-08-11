namespace Club.Entities;

public class ContractToken
{
    public int ContractId { get; set; }
    public required Contract Contract { get; set; }
    public int TokenId { get; set; }
    public required Token Token { get; set; }
}
