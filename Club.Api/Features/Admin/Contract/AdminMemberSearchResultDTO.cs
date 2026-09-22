namespace Club.Features.Admin.Contract;

public class AdminMemberSearchResultDTO
{
    public required Guid UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    // True when the found user already holds this contract, so the UI can disable "Add".
    public bool IsExistingMember { get; set; }
}
