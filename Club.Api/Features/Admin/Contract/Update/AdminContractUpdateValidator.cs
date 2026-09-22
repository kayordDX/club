using FluentValidation;

namespace Club.Features.Admin.Contract.Update;

public class AdminContractUpdateValidator : Validator<AdminContractUpdateRequest>
{
    public AdminContractUpdateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(250).WithMessage("Name cannot exceed 250 characters.");

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(x => x.Frequency).GreaterThan(0).WithMessage("Frequency must be greater than zero.");

        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date cannot be before the start date.");
    }
}
