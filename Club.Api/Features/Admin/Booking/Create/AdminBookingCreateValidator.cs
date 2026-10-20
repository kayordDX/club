using Club.Common.Enums;
using FluentValidation;

namespace Club.Features.Admin.Booking.Create;

public class AdminBookingCreateValidator : Validator<AdminBookingCreateRequest>
{
    public AdminBookingCreateValidator()
    {
        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    if (request.UserId == Guid.Empty)
                        context.AddFailure(nameof(request.UserId), "Select a valid customer account.");
                    if (!request.UserId.HasValue && string.IsNullOrWhiteSpace(request.GuestName))
                        context.AddFailure(nameof(request.GuestName), "A guest name is required when no customer is selected.");
                    if (request.UserId.HasValue && (request.GuestName is not null || request.GuestEmail is not null || request.GuestPhone is not null))
                        context.AddFailure(nameof(request.UserId), "Choose either an existing customer or a guest, not both.");
                }
            );
        RuleFor(x => x.GuestName).MaximumLength(200);
        RuleFor(x => x.GuestEmail).MaximumLength(256).EmailAddress();
        RuleFor(x => x.GuestPhone).MaximumLength(50);
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
