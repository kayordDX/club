using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Features.Booking.Create;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Booking.Common;

public sealed record BookingCreationResult(int? BookingId, int StatusCode, string? Error)
{
    public bool Succeeded => BookingId.HasValue;
}

/// <summary>Shared transactional booking-creation rules, with owner and policy supplied by the authorised adapter.</summary>
public static class BookingCreationMutation
{
    public static async Task<BookingCreationResult> CreateAsync(
        AppDbContext db,
        BookingCreateRequest request,
        Guid ownerId,
        int statusId,
        int? expectedFacilityId,
        Guid? actingAdminId,
        bool allowManagerContractBypass,
        int pendingTimeoutMinutes,
        CancellationToken ct
    )
    {
        if (request.Bookings is null || request.Bookings.Count == 0)
            return new(null, 400, "At least one booking is required.");
        if (request.Extras is null)
            return new(null, 400, "Extras must be provided.");

        if (statusId is not ((int)BookingStatusEnum.Pending) and not ((int)BookingStatusEnum.Confirmed))
            return new(null, 400, "Only Pending and Confirmed bookings can be created.");

        var slotIds = request.Bookings.Select(x => x.SlotId).Distinct().ToList();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var lockedSlots = await ReservationRules.LockSlotsAsync(db, slotIds, ct);
        var slotsById = lockedSlots.ToDictionary(x => x.Id);
        var slotContractIds = request.Bookings.Select(x => x.SlotContractId).Distinct().ToList();
        var slotContracts = await db
            .SlotContract.AsNoTracking()
            .Include(x => x.Contract)
            .Where(x => slotContractIds.Contains(x.Id) && slotIds.Contains(x.SlotId))
            .ToListAsync(ct);
        foreach (var slotContract in slotContracts)
            slotContract.Slot = slotsById.GetValueOrDefault(slotContract.SlotId)!;
        foreach (var item in request.Bookings)
        {
            var contract = slotContracts.FirstOrDefault(x => x.Id == item.SlotContractId && x.SlotId == item.SlotId);
            if (contract is null)
                return new(null, 404, $"SlotContract {item.SlotContractId} not found for slot {item.SlotId}.");
            if (expectedFacilityId.HasValue && contract.Slot.FacilityId != expectedFacilityId)
                return new(null, 404, "Slot contract not found in this facility.");
            if (expectedFacilityId.HasValue && contract.Slot.StartDatetime <= DateTime.UtcNow)
                return new(null, 409, "Bookings can only be created for future available slots.");
        }

        var facilityIds = slotContracts.Select(x => x.Slot.FacilityId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (
            facilityIds.Count != 1
            || slotContracts.Any(x => x.Slot.FacilityId == null)
            || (expectedFacilityId.HasValue && facilityIds[0] != expectedFacilityId)
        )
            return new(null, 400, "All slots must belong to the same facility.");

        var ownerContracts = await db.UserContract.Where(x => x.UserId == ownerId && x.IsActive).ToListAsync(ct);
        HashSet<int> managerFacilityIds = [];
        if (allowManagerContractBypass)
            managerFacilityIds = await db
                .UserRoles.Where(x =>
                    x.UserId == ownerId
                    && x.FacilityId.HasValue
                    && facilityIds.Contains(x.FacilityId.Value)
                    && x.Role.NormalizedName == Constants.Policy.Manager.ToUpperInvariant()
                )
                .Select(x => x.FacilityId!.Value)
                .ToHashSetAsync(ct);

        foreach (var item in request.Bookings)
        {
            var contract = slotContracts.First(x => x.Id == item.SlotContractId && x.SlotId == item.SlotId);
            var isManager = contract.Slot.FacilityId.HasValue && managerFacilityIds.Contains(contract.Slot.FacilityId.Value);
            if (!isManager && !ContractEligibility.IsAllowed(contract.Contract, contract.Slot.StartDatetime, ownerContracts))
                return new(
                    null,
                    allowManagerContractBypass ? 404 : 400,
                    $"Contract {contract.Contract.Name} is not available to the booking owner for slot {item.SlotId}."
                );
        }

        var groupedExtras = request.Extras.GroupBy(x => x.ExtraId).Select(x => new { ExtraId = x.Key, Amount = x.Sum(y => y.Amount) }).ToList();
        if (request.Extras.Any(x => x.Amount <= 0) || groupedExtras.Any(x => x.Amount <= 0))
            return new(null, 400, "Extra amounts must be greater than zero.");

        var extras = groupedExtras.Count == 0 ? [] : await db.Extra.Where(x => groupedExtras.Select(y => y.ExtraId).Contains(x.Id)).ToListAsync(ct);
        foreach (var requested in groupedExtras)
        {
            var extra = extras.FirstOrDefault(x => x.Id == requested.ExtraId);
            if (extra is null || !extra.IsAvailable || !extra.IsOnline || extra.FacilityId != facilityIds[0])
                return new(null, allowManagerContractBypass ? 404 : 400, $"Extra {requested.ExtraId} is not available for this booking.");
        }

        var now = DateTime.UtcNow;
        var activeCounts = await db
            .SlotContractBooking.ActiveReservations(now)
            .Where(x => slotIds.Contains(x.SlotContract.SlotId))
            .GroupBy(x => x.SlotContract.SlotId)
            .Select(x => new { SlotId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.SlotId, x => x.Count, ct);
        foreach (var group in request.Bookings.GroupBy(x => x.SlotId))
        {
            var slot = slotContracts.First(x => x.SlotId == group.Key).Slot;
            var available = slot.MaxBookings - activeCounts.GetValueOrDefault(group.Key, 0);
            if (available < group.Count())
                return new(null, 409, $"Not enough availability for slot {group.Key}. Only {available} slot(s) remaining.");
        }

        var total =
            request.Bookings.Sum(x => slotContracts.First(y => y.Id == x.SlotContractId && y.SlotId == x.SlotId).Price)
            + groupedExtras.Sum(x => extras.First(y => y.Id == x.ExtraId).Price * x.Amount);
        var booking = new Entities.Booking
        {
            FacilityId = facilityIds[0],
            BookingStatusId = statusId,
            BookingStatusDate = now,
            IsPaid = false,
            AmountOutstanding = total,
            AmountPaid = 0,
            ExpiresAt = now.AddMinutes(Math.Max(1, pendingTimeoutMinutes)),
            UserId = ownerId,
        };
        await db.Booking.AddAsync(booking, ct);
        await db.SaveChangesAsync(ct);
        await db.SlotContractBooking.AddRangeAsync(
            request.Bookings.Select(x => new SlotContractBooking
            {
                SlotContractId = x.SlotContractId,
                BookingId = booking.Id,
                Name = x.Name,
                Email = x.Email,
                Cellphone = x.Cellphone,
            }),
            ct
        );
        await db.ExtraBooking.AddRangeAsync(
            groupedExtras.Select(x => new ExtraBooking
            {
                ExtraId = x.ExtraId,
                BookingId = booking.Id,
                Amount = x.Amount,
            }),
            ct
        );
        if (actingAdminId.HasValue)
            db.BookingAction.Add(
                new BookingAction
                {
                    BookingId = booking.Id,
                    ActingAdminId = actingAdminId.Value,
                    Kind = BookingActionKind.Created,
                    Timestamp = now,
                }
            );
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(booking.Id, 200, null);
    }
}
