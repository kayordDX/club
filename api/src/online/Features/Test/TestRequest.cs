using FluentValidation;

namespace Online.Features.Test;

public class TestRequest
{
    public string Name { get; set; } = string.Empty;
}

public class Validator : Validator<TestRequest>
{
    public Validator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
    }
}
