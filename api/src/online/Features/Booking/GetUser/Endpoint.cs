using Online.Common;
using Online.Common.Extensions;
using Online.Common.Models;
using Online.Data;
using Online.DTO;

namespace Online.Features.Booking.GetUser;

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
        var results = await _dbContext.Booking
            .Where(x => x.UserId == userId)
            .ProjectToDto()
            .OrderBy(x => x.Id)
            .GetPagedAsync(r, ct);

        await Send.OkAsync(results, ct);
    }
}
