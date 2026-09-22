namespace Club.Features.Admin.Contract.AddMember;

public class AdminContractAddMemberRequest
{
    public int FacilityId { get; set; }
    public int Id { get; set; }
    public required Guid UserId { get; set; }
}
