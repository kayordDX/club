namespace Club.Entities;

public class TokenType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsExtra { get; set; } = false;
}
