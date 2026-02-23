using FluentValidation;

namespace Online.Features.Account.Role;

public class Request
{
    public string Name { get; set; } = string.Empty;
}

public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
    }
}
