using Club.Common.Enums;
using Club.Data;
using Club.DTO;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Booking.Get;

public class Endpoint(AppDbContext dbContext) : Endpoint<BookingGetRequest, BookingDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/booking/{Id}");
        Description(x => x.WithName("BookingGet"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(BookingGetRequest req, CancellationToken ct)
    {
        var results = await _dbContext.Booking.ProjectToDto().FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (results == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (results.BookingStatusId == (int)BookingStatusEnum.Pending && results.ExpiresAt < DateTime.UtcNow)
        {
            results.BookingStatusId = (int)BookingStatusEnum.Expired;
            if (results.BookingStatus != null)
            {
                results.BookingStatus.Id = (int)BookingStatusEnum.Expired;
                results.BookingStatus.Name = "Expired";
            }
        }

        await Send.OkAsync(results, ct);
    }
}
