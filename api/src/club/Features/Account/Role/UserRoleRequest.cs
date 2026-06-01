using FluentValidation;

namespace Online.Features.Account.Role;

public class UserRoleRequest
{
    public string Name { get; set; } = string.Empty;
}

public class Validator : Validator<UserRoleRequest>
{
    public Validator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
    }
}
