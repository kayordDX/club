using FluentValidation;

namespace Club.Features.Account.Role;

public class AccountRoleRequest
{
    public int FacilityId { get; set; }
}

public class Validator : Validator<AccountRoleRequest>
{
    public Validator()
    {
        RuleFor(x => x.FacilityId).GreaterThan(0).WithMessage("FacilityId is required");
    }
}
