using Microsoft.AspNetCore.Identity;

namespace Online.Entities;

public class User : IdentityUser<int>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Picture { get; set; }

    public override string ToString()
    {
        return FirstName + " " + LastName;
    }
}