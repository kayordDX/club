using Club.Common;
using Club.Common.Config;
using Club.Common.Enums;
using Club.Data;
using Club.Entities;
using Club.Features.Booking.Common;
using Club.Features.Booking.Create;
using Club.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Club.Features.Admin.Booking.Create;

public class Endpoint(AppDbContext dbContext, UserManager<User> userManager, ICustomKeycloakService keycloakService, IOptions<AppConfig> appConfig)
    : Endpoint<AdminBookingCreateRequest, BookingCreateResponse>
{
    public override void Configure()
    {
        Post("/admin/facility/{FacilityId}/booking");
        Description(x => x.WithName("AdminBookingCreate"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminBookingCreateRequest req, CancellationToken ct)
    {
        if (req.UserId == Guid.Empty)
        {
            AddError(r => r.UserId, "A booking owner is required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var owner = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == req.UserId, ct);
        if (owner is null)
        {
            var keycloakUser = await keycloakService.GetUserAsync(req.UserId, ct);
            if (keycloakUser is null)
            {
                AddError(r => r.UserId, "Existing customer account not found.");
                await Send.ErrorsAsync(404, ct);
                return;
            }

            owner = new User
            {
                Id = keycloakUser.Id,
                UserName = keycloakUser.Email ?? keycloakUser.Id.ToString(),
                Email = keycloakUser.Email,
                PhoneNumber = keycloakUser.PhoneNumber,
                FirstName = keycloakUser.FirstName ?? keycloakUser.Email ?? "",
                LastName = keycloakUser.LastName ?? "",
                LastSync = DateTime.UtcNow,
            };
            var syncResult = await userManager.CreateAsync(owner);
            if (!syncResult.Succeeded)
            {
                AddError(r => r.UserId, "Could not synchronise the existing customer account.");
                await Send.ErrorsAsync(409, ct);
                return;
            }
        }

        var actorId = Helpers.GetCurrentUserId(HttpContext);
        if (!actorId.HasValue)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var statusId = req.BookingStatusId ?? (int)BookingStatusEnum.Confirmed;
        if (statusId is not ((int)BookingStatusEnum.Pending) and not ((int)BookingStatusEnum.Confirmed))
        {
            AddError(r => r.BookingStatusId, "Only Pending and Confirmed bookings can be created.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var request = new BookingCreateRequest { Bookings = req.Bookings, Extras = req.Extras };
        var result = await BookingCreationMutation.CreateAsync(
            dbContext,
            request,
            owner.Id,
            statusId,
            new BookingCreationPolicy
            {
                ExpectedFacilityId = req.FacilityId,
                ActingAdminId = actorId,
                PendingTimeoutMinutes = appConfig.Value.PendingTimeoutMinutes,
            },
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
