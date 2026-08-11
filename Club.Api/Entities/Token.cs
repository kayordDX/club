namespace Club.Entities;

public class Token
{
    public int Id { get; set; }
    public int TokenTypeId { get; set; }
    public required TokenType TokenType { get; set; }
    public decimal Amount { get; set; }
}
