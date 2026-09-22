using FluentValidation;

namespace Club.Features.Admin.Contract.UpdateMember;

public class AdminContractUpdateMemberValidator : Validator<AdminContractUpdateMemberRequest>
{
    public AdminContractUpdateMemberValidator()
    {
        RuleFor(x => x.EndDate).NotEmpty().WithMessage("End date is required.");
    }
}
