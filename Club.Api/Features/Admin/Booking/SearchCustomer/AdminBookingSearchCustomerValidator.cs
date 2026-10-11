using FluentValidation;

namespace Club.Features.Admin.Booking.SearchCustomer;

public class AdminBookingSearchCustomerValidator : Validator<AdminBookingSearchCustomerRequest>
{
    public AdminBookingSearchCustomerValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MinimumLength(3).MaximumLength(256);
    }
}
