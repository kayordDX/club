using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Features.Booking.Create;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Booking.Common;

public sealed record BookingMutationFailure(int StatusCode, string Message);

public sealed record BookingSelections(int FacilityId, decimal Total, List<ExtraBooking> Extras);

/// <summary>Selection, pricing and reservation rules shared by creation and editing. Call inside a transaction.</summary>
public static class BookingMutationValidation
{
    public static async Task<(BookingSelections? Selections, BookingMutationFailure? Failure)> ValidateAsync(
        AppDbContext db,
        BookingCreateRequest request,
        Guid? ownerId,
        int? expectedFacilityId,
        bool allowManagerContractBypass,
        bool requireFutureSlots,
        Entities.Booking? existingBooking,
        IReadOnlyList<SlotContractBooking> existingPlayers,
        CancellationToken ct
    )
    {
        if (request.Bookings is null || request.Bookings.Count == 0)
            return (null, new(400, "At least one booking is required."));
        if (request.Extras is null)
            return (null, new(400, "Extras must be provided."));

        var slotIds = request.Bookings.Select(x => x.SlotId).Distinct().ToArray();
        var previousSlotIds = existingPlayers.Select(x => x.SlotContract.SlotId).ToHashSet();
        var slots = await ReservationRules.LockSlotsAsync(db, slotIds.Concat(previousSlotIds), ct);
        var slotsById = slots.ToDictionary(x => x.Id);
        var contractIds = request.Bookings.Select(x => x.SlotContractId).Distinct().ToArray();
        var contracts = await db.SlotContract.AsNoTracking().Include(x => x.Contract).Where(x => contractIds.Contains(x.Id)).ToListAsync(ct);
        foreach (var contract in contracts)
            contract.Slot = slotsById.GetValueOrDefault(contract.SlotId)!;
        for (var index = 0; index < request.Bookings.Count; index++)
        {
            var player = request.Bookings[index];
            var contract = contracts.FirstOrDefault(x => x.Id == player.SlotContractId && x.SlotId == player.SlotId);
            if (contract?.Slot is null || (expectedFacilityId.HasValue && contract.Slot.FacilityId != expectedFacilityId))
                return (null, new(404, $"SlotContract {player.SlotContractId} not found for slot {player.SlotId} in this facility."));
            // A new destination must be available on the schedule; edits within an existing source slot
            // remain allowed after its start time, subject to eligibility, capacity and balance checks.
            if (
                (requireFutureSlots || (existingBooking is not null && !previousSlotIds.Contains(player.SlotId)))
                && contract.Slot.StartDatetime <= DateTime.UtcNow
            )
                return (null, new(409, "Bookings can only be created or moved to future available slots."));
        }

        var selectedContracts = request.Bookings.Select(x => contracts.First(y => y.Id == x.SlotContractId)).ToList();
        var facilityIds = selectedContracts.Select(x => x.Slot.FacilityId).Distinct().ToArray();
        if (facilityIds.Length != 1 || !facilityIds[0].HasValue)
            return (null, new(400, "All slots must belong to the same facility."));
        var facilityId = facilityIds[0]!.Value;
        var ownerContracts = await db.UserContract.Where(x => x.UserId == ownerId && x.IsActive).ToListAsync(ct);
        var isManager =
            allowManagerContractBypass
            && await db.UserRoles.AnyAsync(
                x => x.UserId == ownerId && x.FacilityId == facilityId && x.Role.NormalizedName == Constants.Policy.Manager.ToUpperInvariant(),
                ct
            );
        // The update wire contract carries no player identity, so an unchanged contract is matched by
        // instance count, not list position. A contract keeps at most its existing count without
        // re-checking owner eligibility; any instance beyond that (newly added or re-pointed) is a new
        // benefit that must satisfy current eligibility. Removing or reordering a preceding player
        // therefore never re-qualifies a retained contract.
        var preservedRemaining = existingPlayers.GroupBy(x => (x.SlotContractId, x.SlotContract.SlotId)).ToDictionary(x => x.Key, x => x.Count());
        for (var index = 0; index < request.Bookings.Count; index++)
        {
            var player = request.Bookings[index];
            var contract = selectedContracts[index];
            var key = (player.SlotContractId, player.SlotId);
            var unchanged = preservedRemaining.TryGetValue(key, out var remaining) && remaining > 0;
            if (unchanged)
                preservedRemaining[key] = remaining - 1;
            if (!unchanged && !isManager && !ContractEligibility.IsAllowed(contract.Contract, contract.Slot.StartDatetime, ownerContracts))
                return (
                    null,
                    new(
                        allowManagerContractBypass ? 404 : 400,
                        $"Contract {contract.Contract.Name} is not available to the booking owner for player {index + 1}."
                    )
                );
        }

        var requestedExtras = request.Extras.GroupBy(x => x.ExtraId).Select(x => new ExtraBooking { ExtraId = x.Key, Amount = x.Sum(y => y.Amount) }).ToList();
        if (request.Extras.Any(x => x.Amount <= 0) || requestedExtras.Any(x => x.Amount <= 0))
            return (null, new(400, "Extra amounts must be greater than zero."));
        var extraIds = requestedExtras.Select(x => x.ExtraId).ToArray();
        var extras = await db.Extra.AsNoTracking().Where(x => extraIds.Contains(x.Id)).ToListAsync(ct);
        foreach (var requested in requestedExtras)
        {
            var extra = extras.FirstOrDefault(x => x.Id == requested.ExtraId);
            if (extra is null || !extra.IsAvailable || !extra.IsOnline || extra.FacilityId != facilityId)
                return (null, new(allowManagerContractBypass ? 404 : 400, $"Extra {requested.ExtraId} is not available for this booking."));
        }

        var now = DateTime.UtcNow;
        var existingBookingId = existingBooking?.Id ?? 0;
        if (existingBooking is null || ReservationRules.IsActive(existingBooking, now))
        {
            var counts = await db
                .SlotContractBooking.ActiveReservations(now)
                .Where(x => slotIds.Contains(x.SlotContract.SlotId) && x.BookingId != existingBookingId)
                .GroupBy(x => x.SlotContract.SlotId)
                .Select(x => new { SlotId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.SlotId, x => x.Count, ct);
            foreach (var group in request.Bookings.GroupBy(x => x.SlotId))
            {
                var available = Math.Max(0, slotsById[group.Key].MaxBookings - counts.GetValueOrDefault(group.Key));
                if (group.Count() > available)
                    return (null, new(409, $"Not enough availability for slot {group.Key}. Only {available} place(s) remaining."));
            }
        }

        var total = selectedContracts.Sum(x => x.Price) + requestedExtras.Sum(x => extras.First(y => y.Id == x.ExtraId).Price * x.Amount);
        if (existingBooking is not null)
        {
            var pending = await db
                .PaymentBooking.Where(x => x.BookingId == existingBooking.Id && x.Payment.PaymentStatusId == (int)PaymentStatusEnum.Pending)
                .SumAsync(x => x.Payment.Amount, ct);
            if (total < existingBooking.AmountPaid + pending)
                return (null, new(400, "Booking total cannot be less than the amount already paid or reserved by pending payments."));
        }
        return (new(facilityId, total, requestedExtras), null);
    }
}
