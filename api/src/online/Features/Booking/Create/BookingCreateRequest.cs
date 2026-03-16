using FluentValidation;

namespace Online.Features.Booking.Create;

public class BookingCreateRequest
{
    public Guid SlotId { get; set; }
    public int SlotContractId { get; set; }
    public int Quantity { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class Validator : Validator<BookingCreateRequest>
{
    public Validator()
    {
        RuleFor(x => x.SlotId)
            .NotEmpty()
            .WithMessage("SlotId is required");

        RuleFor(x => x.SlotContractId)
            .GreaterThan(0)
            .WithMessage("SlotContractId is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be at least 1");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid email is required");
    }
}
