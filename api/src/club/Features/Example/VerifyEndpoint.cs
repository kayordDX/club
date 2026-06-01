using Microsoft.AspNetCore.Identity;
using Online.Data;
using Online.Entities;

namespace Online.Features.Example;

public class VerifyEndpoint(AppDbContext dbContext, UserManager<User> userManager) : Endpoint<ExampleRequest, bool>
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly UserManager<User> _userManager = userManager;

    public override void Configure()
    {
        Get("/example/verify");
        Description(x => x.WithName("ExampleVerify"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(ExampleRequest r, CancellationToken ct)
    {
        string email = "kokjaco2@gmail.com";
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            bool result = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, r.Code);
            await Send.OkAsync(result, ct);
            return;

        }
        await Send.OkAsync(false, ct);
    }
}
