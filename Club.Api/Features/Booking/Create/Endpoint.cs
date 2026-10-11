using Club.Common;
using Club.Common.Config;
using Club.Common.Enums;
using Club.Data;
using Club.Features.Booking.Common;
using Microsoft.Extensions.Options;

namespace Club.Features.Booking.Create;

public class Endpoint(AppDbContext dbContext, IOptions<AppConfig> appConfig) : Endpoint<BookingCreateRequest, BookingCreateResponse>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly AppConfig _appConfig = appConfig.Value;

    public override void Configure()
    {
        Post("/booking");
        Description(x => x.WithName("BookingCreate"));
    }

    public override async Task HandleAsync(BookingCreateRequest req, CancellationToken ct)
    {
        var userId = Helpers.GetCurrentUserId(HttpContext);
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var result = await BookingCreationMutation.CreateAsync(
            _dbContext,
            req,
            userId.Value,
            (int)BookingStatusEnum.Pending,
            new BookingCreationPolicy { AllowManagerContractBypass = true, PendingTimeoutMinutes = _appConfig.PendingTimeoutMinutes },
            ct
        );
        if (!result.Succeeded)
        {
            AddError(result.Error!);
            await Send.ErrorsAsync(result.StatusCode, ct);
            return;
        }

        await Send.OkAsync(new BookingCreateResponse { Id = result.BookingId!.Value }, ct);
    }
}
