using Club.Entities;

namespace Club.Features.Booking.Common;

public static class ContractEligibility
{
    /// <summary>
    /// A contract can be booked when it is public, or the user holds a UserContract for it
    /// that is active on the date of the booking.
    /// </summary>
    public static bool IsAllowed(Contract contract, DateTime slotStartDatetime, IEnumerable<UserContract> userContracts) =>
        contract.IsPublic
        || userContracts.Any(uc =>
            uc.ContractId == contract.Id
            && uc.StartDate.Date <= slotStartDatetime.Date
            && (uc.EndDate == null || slotStartDatetime.Date <= uc.EndDate.Value.Date)
        );
}
