namespace Club.Features.Booking.Common;

public sealed record BookingCreationPolicy
{
    public int? ExpectedFacilityId { get; init; }
    public Guid? ActingAdminId { get; init; }
    public string? GuestName { get; init; }
    public string? GuestEmail { get; init; }
    public string? GuestPhone { get; init; }
    public bool AllowManagerContractBypass { get; init; }
    public required int PendingTimeoutMinutes { get; init; }
}
