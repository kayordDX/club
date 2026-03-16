namespace Online.Common;

public static class BookingConstants
{
    public const string PendingStatus = "Pending";
    public const string ConfirmedStatus = "Confirmed";
    public const string CancelledStatus = "Cancelled";
    public static readonly TimeSpan PendingTimeout = TimeSpan.FromMinutes(10);
}
