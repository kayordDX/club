using Club.Common;
using Club.Common.Enums;
using Club.Common.Extensions;
using Club.Common.Models;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Booking.GetUser;

public class Endpoint(AppDbContext dbContext) : Endpoint<BookingGetUserRequest, PaginatedList<BookingSummaryDTO>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/booking/user");
        Description(x => x.WithName("BookingGetUser"));
    }

    public override async Task HandleAsync(BookingGetUserRequest r, CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (userId == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var now = DateTime.UtcNow;
        var query = _dbContext
            .Booking.Where(b => b.UserId == userId)
            .Select(b => new BookingSummaryDTO
            {
                Id = b.Id,
                // Expired is a derived state: pending bookings past their expiry read as Expired
                // so they can be filtered and displayed consistently with the rest of the statuses.
                BookingStatusId =
                    (b.BookingStatusId == (int)BookingStatusEnum.Pending && b.ExpiresAt < now) ? (int)BookingStatusEnum.Expired : b.BookingStatusId,
                BookingStatusName =
                    (b.BookingStatusId == (int)BookingStatusEnum.Pending && b.ExpiresAt < now) ? nameof(BookingStatusEnum.Expired) : b.BookingStatus.Name,
                BookingStatusDate = b.BookingStatusDate,
                FacilityName = b.SlotContractBookings.Select(scb => scb.SlotContract.Slot.Facility!.Name).FirstOrDefault(),
                SlotStartDatetime = b.SlotContractBookings.Min(scb => (DateTime?)scb.SlotContract.Slot.StartDatetime),
                SlotEndDatetime = b.SlotContractBookings.Max(scb => scb.SlotContract.Slot.EndDatetime),
                PlayerCount = b.SlotContractBookings.Count,
                AmountOutstanding = b.AmountOutstanding,
                AmountPaid = b.AmountPaid,
                IsPaid = b.IsPaid,
                ExpiresAt = b.ExpiresAt,
            })
            .OrderBy(x => x.Id);

        var results = await query.GetPagedAsync(r, ct);

        await Send.OkAsync(results, ct);
    }
}
