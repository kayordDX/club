
using Microsoft.AspNetCore.Identity;
using Online.Entities;

namespace Online.Features.Account.Passkey;

public class Endpoint(UserManager<User> userManager, SignInManager<User> signInManager) : Endpoint<PasskeyRequest, string>
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly SignInManager<User> _signInManager = signInManager;

    public override void Configure()
    {
        Post("/account/passkey");
        Description(x => x.WithName("Passkey"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(PasskeyRequest r, CancellationToken ct)
    {

        var user = await _userManager.GetUserAsync(HttpContext.User);

        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var userName = await _userManager.GetUserNameAsync(user) ?? "User";

        var optionsJson = await _signInManager.MakePasskeyCreationOptionsAsync(new()
        {
            Id = userId,
            Name = userName,
            DisplayName = userName
        });

        var attestationResult = await _signInManager.PerformPasskeyAttestationAsync(optionsJson);

        await Send.OkAsync(optionsJson);
    }
}
