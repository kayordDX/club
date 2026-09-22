using FluentValidation;

namespace Club.Features.Admin.Contract.SearchMember;

public class AdminContractSearchMemberValidator : Validator<AdminContractSearchMemberRequest>
{
    public AdminContractSearchMemberValidator()
    {
        RuleFor(x => x.Query).NotEmpty().WithMessage("An email address or cellphone number is required.");
    }
}
