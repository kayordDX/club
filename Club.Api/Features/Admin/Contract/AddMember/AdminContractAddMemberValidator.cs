using FluentValidation;

namespace Club.Features.Admin.Contract.AddMember;

public class AdminContractAddMemberValidator : Validator<AdminContractAddMemberRequest>
{
    public AdminContractAddMemberValidator()
    {
        RuleFor(x => x.EndDate).NotEmpty().WithMessage("End date is required.");
    }
}
