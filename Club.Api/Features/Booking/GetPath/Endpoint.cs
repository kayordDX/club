using Club.Common;
using Club.Data;
using Club.Features.Booking.Common;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Booking.GetPath;

public class Endpoint(AppDbContext dbContext) : Endpoint<BookingGetPathRequest, BookingPathDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/booking/{Id}/path");
        Description(x => x.WithName("BookingGetPath"));
    }

    public override async Task HandleAsync(BookingGetPathRequest req, CancellationToken ct)
    {
        if (Helpers.GetCurrentUserId(HttpContext) == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var userId = Helpers.GetCurrentUserId(HttpContext);
        var path = await _dbContext.Booking.Where(b => b.Id == req.Id && b.UserId == userId && b.FacilityId != null).ProjectToPath().FirstOrDefaultAsync(ct);

        if (path == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(path, ct);
    }
}
