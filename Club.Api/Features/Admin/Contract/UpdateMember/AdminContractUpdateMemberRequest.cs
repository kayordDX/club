namespace Club.Features.Admin.Contract.UpdateMember;

public class AdminContractUpdateMemberRequest
{
    public int FacilityId { get; set; }
    public int Id { get; set; }
    public int MemberId { get; set; }
    public DateTime EndDate { get; set; }
}
