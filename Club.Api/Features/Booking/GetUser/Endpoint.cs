using Club.Common;
using Club.Common.Enums;
using Club.Common.Extensions;
using Club.Common.Models;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Booking.GetUser;

public class Endpoint(AppDbContext dbContext) : Endpoint<BookingGetUserRequest, PaginatedList<BookingDTO>>
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
        var results = await _dbContext.Booking.Where(x => x.UserId == userId).AsSplitQuery().ProjectToDto().OrderBy(x => x.Id).GetPagedAsync(r, ct);

        var now = DateTime.UtcNow;
        foreach (var booking in results.Items)
        {
            if (booking.BookingStatusId == (int)BookingStatusEnum.Pending && booking.ExpiresAt < now)
            {
                booking.BookingStatusId = (int)BookingStatusEnum.Expired;
                if (booking.BookingStatus != null)
                {
                    booking.BookingStatus.Id = (int)BookingStatusEnum.Expired;
                    booking.BookingStatus.Name = "Expired";
                }
            }
        }

        await Send.OkAsync(results, ct);
    }
}
