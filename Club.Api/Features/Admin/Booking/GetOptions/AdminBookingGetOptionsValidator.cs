using FluentValidation;

namespace Club.Features.Admin.Booking.GetOptions;

public class AdminBookingGetOptionsValidator : Validator<AdminBookingGetOptionsRequest>
{
    public AdminBookingGetOptionsValidator()
    {
        RuleFor(x => x.SlotId).NotEmpty();
        RuleFor(x => x.UserId).Must(x => x != Guid.Empty).WithMessage("Select a valid customer account.");
    }
}
