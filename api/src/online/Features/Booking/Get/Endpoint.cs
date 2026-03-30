using Microsoft.EntityFrameworkCore;
using Online.Data;
using Online.DTO;

namespace Online.Features.Booking.Get;

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
        await Send.OkAsync(results, ct);
    }
}
