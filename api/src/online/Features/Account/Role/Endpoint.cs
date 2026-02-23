using Microsoft.EntityFrameworkCore;
using Online.Data;
using Online.Entities;
using Microsoft.AspNetCore.Identity;

namespace Online.Features.Account.Role;

public class Endpoint(UserManager<User> userManager, UserStore userStore) : Endpoint<Request, bool>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly UserStore _userStore = userStore;
    public override void Configure()
    {
        Post("/user/role");
        Description(x => x.WithName("UserRole"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
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
