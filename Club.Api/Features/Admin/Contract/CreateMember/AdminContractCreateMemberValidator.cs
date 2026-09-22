using FluentValidation;

namespace Club.Features.Admin.Contract.CreateMember;

public class AdminContractCreateMemberValidator : Validator<AdminContractCreateMemberRequest>
{
    public AdminContractCreateMemberValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.");

        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.");
    }
}
