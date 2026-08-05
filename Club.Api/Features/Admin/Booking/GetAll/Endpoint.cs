using Club.Common;
using Club.Common.Enums;
using Club.Common.Extensions;
using Club.Common.Models;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Booking.GetAll;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminBookingGetAllRequest, PaginatedList<AdminBookingDTO>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/booking");
        Description(x => x.WithName("AdminBookingGetAll"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminBookingGetAllRequest req, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var query = (
            from b in _dbContext.Booking
            where b.SlotContractBookings.Any(scb => scb.SlotContract.Slot.FacilityId == req.FacilityId)
            select new AdminBookingDTO
            {
                Id = b.Id,
                // Expired is a derived state: pending bookings past their expiry read as Expired
                // so they can be filtered and displayed consistently with the rest of the statuses.
                BookingStatusId =
                    (b.BookingStatusId == (int)BookingStatusEnum.Pending && b.ExpiresAt < now) ? (int)BookingStatusEnum.Expired : b.BookingStatusId,
                BookingStatusName =
                    (b.BookingStatusId == (int)BookingStatusEnum.Pending && b.ExpiresAt < now) ? nameof(BookingStatusEnum.Expired) : b.BookingStatus.Name,
                BookingStatusDate = b.BookingStatusDate,
                SlotStartDatetime = b.SlotContractBookings.Min(scb => (DateTime?)scb.SlotContract.Slot.StartDatetime),
                SlotEndDatetime = b.SlotContractBookings.Max(scb => scb.SlotContract.Slot.EndDatetime),
                UserId = b.UserId,
                CustomerName = b.User != null ? b.User.FirstName + " " + b.User.LastName : null,
                FacilityName = b.SlotContractBookings.Select(scb => scb.SlotContract.Slot.Facility!.Name).FirstOrDefault(),
                PlayerCount = b.SlotContractBookings.Count,
                ExtraCount = b.ExtraBookings.Count,
                IsPaid = b.IsPaid,
                AmountPaid = b.AmountPaid,
                AmountOutstanding = b.AmountOutstanding,
                ExpiresAt = b.ExpiresAt,
            }
        ).OrderByDescending(x => x.SlotStartDatetime ?? DateTime.MinValue);

        var results = await query.GetPagedAsync(req, ct);

        await Send.OkAsync(results, ct);
    }
}
