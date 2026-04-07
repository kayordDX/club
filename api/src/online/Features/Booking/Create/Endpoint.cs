using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Online.Common;
using Online.Common.Config;
using Online.Common.Enums;
using Online.Data;

namespace Online.Features.Booking.Create;

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

        var slotContracts = await _dbContext.SlotContract
            .Include(sc => sc.Slot)
            .Where(sc => slotContractIds.Contains(sc.Id))
            .ToListAsync(ct);

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

        var existingCounts = await _dbContext.SlotContractBooking
            .Where(scb =>
                slotIds.Contains(scb.SlotContract.SlotId) &&
                scb.Booking.BookingStatusId != (int)BookingStatusEnum.Cancelled)
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
        var totalPrice = req.Bookings.Sum(br =>
            slotContracts.First(sc => sc.Id == br.SlotContractId && sc.SlotId == br.SlotId).Price);

        var booking = new Entities.Booking
        {
            BookingStatusId = (int)BookingStatusEnum.Pending,
            BookingStatusDate = now,
            IsPaid = false,
            AmountOutstanding = totalPrice,
            AmountPaid = 0,
            ExpiresAt = now.AddMinutes(10)
        };

        await _dbContext.Booking.AddAsync(booking, ct);
        await _dbContext.SaveChangesAsync(ct);

        var slotContractBookings = req.Bookings
            .Select(br => new Entities.SlotContractBooking
            {
                SlotContractId = br.SlotContractId,
                BookingId = booking.Id,
                Name = br.Name,
                Email = br.Email,
                Cellphone = br.Cellphone,
            })
            .ToList();

        await _dbContext.SlotContractBooking.AddRangeAsync(slotContractBookings, ct);
        await _dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new BookingCreateResponse
        {
            Id = booking.Id,
        }, ct);
    }
}
