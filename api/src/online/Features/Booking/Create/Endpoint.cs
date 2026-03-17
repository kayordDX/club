using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Online.Common;
using Online.Common.Config;
using Online.Data;
using Online.Entities;

namespace Online.Features.Booking.Create;

public class Endpoint(AppDbContext dbContext, IOptions<AppConfig> appConfig) : Endpoint<BookingCreateRequest, BookingCreateResponse>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly AppConfig _appConfig = appConfig.Value;

    public override void Configure()
    {
        Post("/booking");
        Description(x => x.WithName("BookingCreate"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(BookingCreateRequest req, CancellationToken ct)
    {

    }
}
