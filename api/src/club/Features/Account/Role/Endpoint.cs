using Microsoft.EntityFrameworkCore;
using Club.Data;
using Club.Entities;
using Microsoft.AspNetCore.Identity;

namespace Club.Features.Account.Role;

public class Endpoint(UserManager<User> userManager, UserStore userStore) : Endpoint<UserRoleRequest, bool>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly UserStore _userStore = userStore;
    public override void Configure()
    {
        Post("/user/role");
        Description(x => x.WithName("UserRole"));
    }

    public override async Task HandleAsync(UserRoleRequest req, CancellationToken ct)
    {
        await Task.Delay(2000, ct);
        var user = await _userManager.GetUserAsync(HttpContext.User);

        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await _userStore.AddToRoleAsync(user, "test", 1, ct);
        await _userStore.AddToRoleAsync(user, "superAdmin", null, ct);

        await Send.OkAsync(true, ct);
    }
}
