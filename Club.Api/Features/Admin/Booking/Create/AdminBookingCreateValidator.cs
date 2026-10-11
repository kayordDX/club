using Club.Common.Enums;
using FluentValidation;

namespace Club.Features.Admin.Booking.Create;

public class AdminBookingCreateValidator : Validator<AdminBookingCreateRequest>
{
    public AdminBookingCreateValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Bookings).NotNull().NotEmpty();
        RuleForEach(x => x.Bookings)
            .ChildRules(player =>
            {
                player.RuleFor(x => x.SlotId).NotEmpty();
                player.RuleFor(x => x.SlotContractId).GreaterThan(0);
            });
        RuleFor(x => x.Extras).NotNull();
        RuleForEach(x => x.Extras)
            .ChildRules(extra =>
            {
                extra.RuleFor(x => x.ExtraId).GreaterThan(0);
                extra.RuleFor(x => x.Amount).GreaterThan(0);
            });
        RuleFor(x => x.BookingStatusId)
            .Must(x => x is null || x == (int)BookingStatusEnum.Pending || x == (int)BookingStatusEnum.Confirmed)
            .WithMessage("Only Pending and Confirmed bookings can be created.");
    }
}
