using Club.Features.Booking.GetPath;

namespace Club.Features.Booking.Common;

public static class BookingPaths
{
    public static IQueryable<BookingPathDTO> ProjectToPath(this IQueryable<Entities.Booking> bookings) =>
        bookings
            .Where(b => b.FacilityId != null)
            .Select(b => new BookingPathDTO
            {
                BookingId = b.Id,
                OutletId = b.Facility!.OutletId,
                OutletSlug = b.Facility.Outlet.Slug,
                OutletName = b.Facility.Outlet.Name,
                FacilityId = b.Facility.Id,
                FacilityName = b.Facility.Name,
                SlotId = b.SlotContractBookings.OrderBy(scb => scb.Id).Select(scb => (Guid?)scb.SlotContract.SlotId).FirstOrDefault(),
                SlotStartDatetime = b
                    .SlotContractBookings.OrderBy(scb => scb.Id)
                    .Select(scb => (DateTime?)scb.SlotContract.Slot.StartDatetime)
                    .FirstOrDefault(),
            });
}
