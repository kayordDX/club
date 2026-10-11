using Club.Data;
using Club.Services;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Booking.SearchCustomer;

public class Endpoint(AppDbContext dbContext, ICustomKeycloakService keycloakService) : Endpoint<AdminBookingSearchCustomerRequest, AdminBookingCustomerDTO?>
{
    public override void Configure()
    {
        Get("/admin/facility/{FacilityId}/booking/customer/search");
        Description(x => x.WithName("AdminBookingSearchCustomer"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminBookingSearchCustomerRequest req, CancellationToken ct)
    {
        if (!await dbContext.Facility.AnyAsync(x => x.Id == req.FacilityId, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var query = req.Query?.Trim();
        if (string.IsNullOrWhiteSpace(query) || query.Length is < 3 or > 256)
        {
            AddError(r => r.Query, "Search with at least 3 characters (maximum 256).");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var local = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == query || x.PhoneNumber == query, ct);
        if (local is not null)
        {
            await Send.OkAsync(
                new AdminBookingCustomerDTO
                {
                    UserId = local.Id,
                    FirstName = local.FirstName,
                    LastName = local.LastName,
                    Email = local.Email,
                    PhoneNumber = local.PhoneNumber,
                },
                ct
            );
            return;
        }

        var keycloakUser = string.IsNullOrWhiteSpace(query) ? null : await keycloakService.FindUserAsync(query, ct);
        await Send.OkAsync(
            keycloakUser is null
                ? null
                : new AdminBookingCustomerDTO
                {
                    UserId = keycloakUser.Id,
                    FirstName = keycloakUser.FirstName,
                    LastName = keycloakUser.LastName,
                    Email = keycloakUser.Email,
                    PhoneNumber = keycloakUser.PhoneNumber,
                },
            ct
        );
    }
}
