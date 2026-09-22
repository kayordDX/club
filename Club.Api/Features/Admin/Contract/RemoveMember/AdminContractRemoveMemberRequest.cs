namespace Club.Features.Admin.Contract.RemoveMember;

public class AdminContractRemoveMemberRequest
{
    public int FacilityId { get; set; }
    public int Id { get; set; }

    // The UserContract id (the membership link), as returned by GetMembers.
    public int MemberId { get; set; }
}
