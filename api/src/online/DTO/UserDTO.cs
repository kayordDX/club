namespace Online.DTO;

public class UserDTO
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Picture { get; set; }
    public override string ToString()
    {
        return FirstName + " " + LastName;
    }
}
