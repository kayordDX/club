using FluentValidation;

namespace Online.Features.Slot.GetAll;

public class SlotGetAllRequest
{
    public int FacilityId { get; set; }
    public required DateTime Date { get; set; }
}

public class Validator : Validator<SlotGetAllRequest>
{
    public Validator()
    {
        RuleFor(x => x.FacilityId).GreaterThan(0).WithMessage("FacilityId is required");
        RuleFor(x => x.Date).NotEmpty().WithMessage("Date is required");
    }
}
