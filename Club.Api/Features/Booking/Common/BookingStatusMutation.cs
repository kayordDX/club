using Club.Common.Enums;
using Club.Common.Payments;
using Club.Data;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Booking.Common;

public static class BookingStatusMutation
{
    public sealed record Failure(int StatusCode, string Message);

    public static async Task<Failure?> ChangeAsync(
        AppDbContext db,
        int bookingId,
        Guid actorId,
        int? adminFacilityId,
        BookingStatusEnum status,
        int pendingTimeoutMinutes,
        CancellationToken ct
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var booking = await BookingPayments.LockAsync(db, bookingId, ct);
        if (booking is null || (adminFacilityId.HasValue && booking.FacilityId != adminFacilityId))
            return new(404, "Booking not found.");
        if (!adminFacilityId.HasValue && booking.UserId != actorId)
            return new(403, "You can only change your own bookings.");

        if (booking.BookingStatusId == (int)status && status != BookingStatusEnum.Pending)
            return null;

        var players = await db.SlotContractBooking.Include(x => x.SlotContract).Where(x => x.BookingId == bookingId).ToListAsync(ct);
        var slots = await ReservationRules.LockSlotsAsync(db, players.Select(x => x.SlotContract.SlotId), ct);
        var now = DateTime.UtcNow;
        var wasActive = ReservationRules.IsActive(booking, now);
        var activates = status is BookingStatusEnum.Pending or BookingStatusEnum.Confirmed;
        if (!wasActive && activates)
        {
            if (!adminFacilityId.HasValue)
                return new(409, "This booking is inactive. Ask a facility manager to restore it.");
            if (players.Count == 0 || slots.Any(x => x.FacilityId != booking.FacilityId))
                return new(409, "This booking has no valid reserved places. It cannot be restored.");
            var slotIds = slots.Select(x => x.Id).ToArray();
            var counts = await db
                .SlotContractBooking.ActiveReservations(now)
                .Where(x => slotIds.Contains(x.SlotContract.SlotId) && x.BookingId != bookingId)
                .GroupBy(x => x.SlotContract.SlotId)
                .Select(g => new { SlotId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SlotId, x => x.Count, ct);
            foreach (var slot in slots)
            {
                var available = Math.Max(0, slot.MaxBookings - counts.GetValueOrDefault(slot.Id));
                if (players.Count(x => x.SlotContract.SlotId == slot.Id) > available)
                    return new(409, $"Not enough availability for slot {slot.Id}. Only {available} place(s) remaining.");
            }
        }

        if (booking.BookingStatusId == (int)status && wasActive)
            return null;
        if (status == BookingStatusEnum.Pending)
            booking.ExpiresAt = now.AddMinutes(Math.Max(1, pendingTimeoutMinutes));
        booking.BookingStatusId = (int)status;
        booking.BookingStatusDate = now;
        if (adminFacilityId.HasValue)
        {
            var kind = status switch
            {
                BookingStatusEnum.Cancelled => BookingActionKind.Cancelled,
                BookingStatusEnum.Expired => BookingActionKind.Expired,
                BookingStatusEnum.Pending => wasActive ? BookingActionKind.Pending : BookingActionKind.RestoredToPending,
                _ => wasActive ? BookingActionKind.Confirmed : BookingActionKind.RestoredToConfirmed,
            };
            db.BookingAction.Add(
                new BookingAction
                {
                    BookingId = bookingId,
                    ActingAdminId = actorId,
                    Kind = kind,
                    Timestamp = now,
                }
            );
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return null;
    }
}
