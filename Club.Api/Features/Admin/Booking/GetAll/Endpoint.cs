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
        var query = (
            from b in _dbContext.Booking
            where b.SlotContractBookings.Any(scb => scb.SlotContract.Slot.FacilityId == req.FacilityId)
            select new AdminBookingDTO
            {
                Id = b.Id,
                BookingStatusId = b.BookingStatusId,
                BookingStatusName = b.BookingStatus.Name,
                BookingStatusDate = b.BookingStatusDate,
                SlotStartDatetime = b.SlotContractBookings.Min(scb => (DateTime?)scb.SlotContract.Slot.StartDatetime),
                UserId = b.UserId,
                CustomerName = b.User != null ? b.User.FirstName + " " + b.User.LastName : null,
                PlayerCount = b.SlotContractBookings.Count,
                ExtraCount = b.ExtraBookings.Count,
                IsPaid = b.IsPaid,
                AmountPaid = b.AmountPaid,
                AmountOutstanding = b.AmountOutstanding,
                ExpiresAt = b.ExpiresAt,
            }
        ).OrderByDescending(x => x.SlotStartDatetime ?? DateTime.MinValue);

        var results = await query.GetPagedAsync(req, ct);

        var now = DateTime.UtcNow;
        foreach (var booking in results.Items)
        {
            if (booking.BookingStatusId == (int)BookingStatusEnum.Pending && booking.ExpiresAt < now)
            {
                booking.BookingStatusId = (int)BookingStatusEnum.Expired;
                booking.BookingStatusName = "Expired";
            }
        }

        await Send.OkAsync(results, ct);
    }
}
