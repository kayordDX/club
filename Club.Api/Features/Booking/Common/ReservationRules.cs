using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Booking.Common;

public static class ReservationRules
{
    public static bool IsActive(Entities.Booking booking, DateTime now) =>
        booking.BookingStatusId == (int)BookingStatusEnum.Confirmed || (booking.BookingStatusId == (int)BookingStatusEnum.Pending && booking.ExpiresAt > now);

    public static IQueryable<SlotContractBooking> ActiveReservations(this IQueryable<SlotContractBooking> query, DateTime now) =>
        query.Where(x =>
            x.Booking.BookingStatusId == (int)BookingStatusEnum.Confirmed
            || (x.Booking.BookingStatusId == (int)BookingStatusEnum.Pending && x.Booking.ExpiresAt > now)
        );

    // Booking mutations lock the booking first, then all affected slots in deterministic order.
    // Creation only needs slot locks. Call inside the mutation's transaction.
    public static Task<List<Entities.Slot>> LockSlotsAsync(AppDbContext db, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var slotIds = ids.Distinct().ToArray();
        return db.Slot.FromSqlInterpolated($"SELECT * FROM slot WHERE id = ANY({slotIds}) ORDER BY id FOR UPDATE").ToListAsync(ct);
    }
}
