using FluentValidation;

namespace Online.Features.Slot.GetContracts;

public class SlotGetContractsRequest
{
    public Guid Id { get; set; }
}

public class Validator : Validator<SlotGetContractsRequest>
{
    public Validator()
    {
        RuleFor(x => x.Id).NotNull().WithMessage("Id is required");
    }
}
