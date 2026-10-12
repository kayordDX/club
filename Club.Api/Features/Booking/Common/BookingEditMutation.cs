using Club.Common.Enums;
using Club.Common.Payments;
using Club.Data;
using Club.Entities;
using Club.Features.Booking.Create;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Booking.Common;

public static class BookingEditMutation
{
    public static async Task<BookingMutationFailure?> EditAsync(
        AppDbContext db,
        int bookingId,
        BookingCreateRequest request,
        Guid actorId,
        int? adminFacilityId,
        int pendingTimeoutMinutes,
        CancellationToken ct
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var booking = await BookingPayments.LockAsync(db, bookingId, ct);
        if (booking is null || (adminFacilityId.HasValue && booking.FacilityId != adminFacilityId))
            return new(404, "Booking not found.");

        // Customer restrictions stay separate from facility-authorised admin policy. Managers editing
        // through the customer adapter still follow customer status, expiry and payment restrictions.
        if (!adminFacilityId.HasValue)
        {
            if (booking.UserId != actorId)
                return new(403, "You can only edit your own bookings.");
            if (booking.BookingStatusId != (int)BookingStatusEnum.Pending)
                return new(400, "Only pending bookings can be edited.");
            if (booking.ExpiresAt <= DateTime.UtcNow)
                return new(400, "The booking has expired and can no longer be edited.");
            if (
                booking.AmountPaid > 0
                || await db.PaymentBooking.AnyAsync(x => x.BookingId == bookingId && x.Payment.PaymentStatusId == (int)PaymentStatusEnum.Pending, ct)
            )
                return new(409, "Bookings with paid or pending payments cannot be edited.");
        }

        var players = await db.SlotContractBooking.Include(x => x.SlotContract).Where(x => x.BookingId == bookingId).OrderBy(x => x.Id).ToListAsync(ct);
        var (selections, failure) = await BookingMutationValidation.ValidateAsync(
            db,
            request,
            booking.UserId,
            adminFacilityId,
            !adminFacilityId.HasValue,
            false,
            booking,
            players,
            ct
        );
        if (failure is not null)
            return failure;

        var previousSlots = players.Select(x => x.SlotContract.SlotId).ToHashSet();
        var destinationSlots = request.Bookings.Select(x => x.SlotId).ToHashSet();
        var moved =
            !previousSlots.SetEquals(destinationSlots)
            || players.Take(request.Bookings.Count).Where((player, index) => player.SlotContract.SlotId != request.Bookings[index].SlotId).Any();
        var extras = await db.ExtraBooking.Where(x => x.BookingId == bookingId).ToListAsync(ct);
        db.SlotContractBooking.RemoveRange(players.Skip(request.Bookings.Count));
        db.ExtraBooking.RemoveRange(extras);
        for (var index = 0; index < request.Bookings.Count; index++)
        {
            var requested = request.Bookings[index];
            var player = index < players.Count ? players[index] : new SlotContractBooking { BookingId = bookingId };
            player.SlotContractId = requested.SlotContractId;
            player.Name = requested.Name;
            player.Email = requested.Email;
            player.Cellphone = requested.Cellphone;
            if (index >= players.Count)
                db.SlotContractBooking.Add(player);
        }
        foreach (var extra in selections!.Extras)
            extra.BookingId = bookingId;
        await db.ExtraBooking.AddRangeAsync(selections.Extras, ct);
        booking.AmountOutstanding = selections.Total - booking.AmountPaid;
        booking.IsPaid = booking.AmountOutstanding == 0;
        if (!adminFacilityId.HasValue)
        {
            booking.FacilityId = selections.FacilityId;
            booking.ExpiresAt = DateTime.UtcNow.AddMinutes(Math.Max(1, pendingTimeoutMinutes));
        }
        else
        {
            db.BookingAction.Add(
                new BookingAction
                {
                    BookingId = bookingId,
                    ActingAdminId = actorId,
                    Kind = moved ? BookingActionKind.Moved : BookingActionKind.Edited,
                    Timestamp = DateTime.UtcNow,
                }
            );
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return null;
    }
}
