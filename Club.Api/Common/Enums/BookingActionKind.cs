namespace Club.Common.Enums;

public enum BookingActionKind
{
    Created = 7,
    Cancelled = 1,
    Confirmed = 2,
    RestoredToPending = 3,
    RestoredToConfirmed = 4,
    Pending = 5,
    Expired = 6,
}
