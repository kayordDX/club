using Club.Common;
using Club.Common.Config;
using Club.Common.Enums;
using Club.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Club.Features.Booking.Create;

public class Endpoint(AppDbContext dbContext) : Endpoint<BookingCreateRequest, BookingCreateResponse>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Post("/booking");
        Description(x => x.WithName("BookingCreate"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(BookingCreateRequest req, CancellationToken ct)
    {
        if (req.Bookings.Count == 0)
        {
            AddError(r => r.Bookings, "At least one booking is required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var slotContractIds = req.Bookings.Select(b => b.SlotContractId).Distinct().ToList();

        var slotContracts = await _dbContext.SlotContract.Include(sc => sc.Slot).Where(sc => slotContractIds.Contains(sc.Id)).ToListAsync(ct);

        foreach (var bookingReq in req.Bookings)
        {
            var sc = slotContracts.FirstOrDefault(sc => sc.Id == bookingReq.SlotContractId && sc.SlotId == bookingReq.SlotId);
            if (sc is null)
                AddError(r => r.Bookings, $"SlotContract {bookingReq.SlotContractId} not found for slot {bookingReq.SlotId}.");
        }

        if (ValidationFailed)
        {
            await Send.ErrorsAsync(404, ct);
            return;
        }

        var slotIds = req.Bookings.Select(b => b.SlotId).Distinct().ToList();
        var facilityIds = slotContracts
            .Select(sc => sc.Slot.FacilityId)
            .Where(facilityId => facilityId.HasValue)
            .Select(facilityId => facilityId!.Value)
            .Distinct()
            .ToList();

        var existingCounts = await _dbContext
            .SlotContractBooking.Where(scb => slotIds.Contains(scb.SlotContract.SlotId) && scb.Booking.BookingStatusId != (int)BookingStatusEnum.Cancelled)
            .GroupBy(scb => scb.SlotContract.SlotId)
            .Select(g => new { SlotId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SlotId, x => x.Count, ct);

        foreach (var slotGroup in req.Bookings.GroupBy(b => b.SlotId))
        {
            var slot = slotContracts.First(sc => sc.SlotId == slotGroup.Key).Slot;
            var existing = existingCounts.GetValueOrDefault(slotGroup.Key, 0);
            var available = slot.MaxBookings - existing;

            if (available < slotGroup.Count())
                AddError(r => r.Bookings, $"Not enough availability for slot {slotGroup.Key}. Only {available} slot(s) remaining.");
        }

        if (ValidationFailed)
        {
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var now = DateTime.UtcNow;
        var requestedExtras = req
            .Extras.GroupBy(extra => extra.ExtraId)
            .Select(group => new { ExtraId = group.Key, Amount = group.Sum(extra => extra.Amount) })
            .ToList();

        if (requestedExtras.Any(extra => extra.Amount <= 0))
        {
            AddError(r => r.Extras, "Extra amounts must be greater than zero.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var extras =
            requestedExtras.Count == 0
                ? []
                : await _dbContext.Extra.Where(extra => requestedExtras.Select(requestedExtra => requestedExtra.ExtraId).Contains(extra.Id)).ToListAsync(ct);

        foreach (var requestedExtra in requestedExtras)
        {
            var extra = extras.FirstOrDefault(item => item.Id == requestedExtra.ExtraId);
            if (extra is null || !extra.IsAvailable || !extra.IsOnline || !facilityIds.Contains(extra.FacilityId))
            {
                AddError(r => r.Extras, $"Extra {requestedExtra.ExtraId} is not available for this booking.");
            }
        }

        if (ValidationFailed)
        {
            await Send.ErrorsAsync(404, ct);
            return;
        }

        var totalPrice = req.Bookings.Sum(br => slotContracts.First(sc => sc.Id == br.SlotContractId && sc.SlotId == br.SlotId).Price);
        var extrasTotal = requestedExtras.Sum(requestedExtra => extras.First(extra => extra.Id == requestedExtra.ExtraId).Price * requestedExtra.Amount);

        var userId = Helpers.GetCurrentUserId(HttpContext);
        var booking = new Entities.Booking
        {
            BookingStatusId = (int)BookingStatusEnum.Pending,
            BookingStatusDate = now,
            IsPaid = false,
            AmountOutstanding = totalPrice + extrasTotal,
            AmountPaid = 0,
            ExpiresAt = now.AddMinutes(10),
            UserId = userId,
        };

        await _dbContext.Booking.AddAsync(booking, ct);
        await _dbContext.SaveChangesAsync(ct);

        var slotContractBookings = req
            .Bookings.Select(br => new Entities.SlotContractBooking
            {
                SlotContractId = br.SlotContractId,
                BookingId = booking.Id,
                Name = br.Name,
                Email = br.Email,
                Cellphone = br.Cellphone,
            })
            .ToList();

        await _dbContext.SlotContractBooking.AddRangeAsync(slotContractBookings, ct);
        var extraBookings = requestedExtras.Select(requestedExtra => new Entities.ExtraBooking
        {
            ExtraId = requestedExtra.ExtraId,
            BookingId = booking.Id,
            Amount = requestedExtra.Amount,
        });
        await _dbContext.ExtraBooking.AddRangeAsync(extraBookings, ct);
        await _dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new BookingCreateResponse { Id = booking.Id }, ct);
    }
}
