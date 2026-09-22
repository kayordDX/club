namespace Club.Features.Admin.Contract.CreateMember;

public class AdminContractCreateMemberRequest
{
    public int FacilityId { get; set; }
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
}
