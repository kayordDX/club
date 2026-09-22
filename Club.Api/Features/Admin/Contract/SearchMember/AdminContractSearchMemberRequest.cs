namespace Club.Features.Admin.Contract.SearchMember;

public class AdminContractSearchMemberRequest
{
    public int FacilityId { get; set; }
    public int Id { get; set; }

    // Email address or cellphone number to look the member up by.
    public string Query { get; set; } = string.Empty;
}
