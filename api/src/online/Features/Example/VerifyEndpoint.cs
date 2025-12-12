using Microsoft.AspNetCore.Identity;
using Online.Data;
using Online.Entities;

namespace Online.Features.Example;

public class VerifyEndpoint : Endpoint<Request, bool>
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<User> _userManager;

    public VerifyEndpoint(AppDbContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public override void Configure()
    {
        Get("/example/verify");
        Description(x => x.WithName("ExampleVerify"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request r, CancellationToken ct)
    {
        string email = "kokjaco2@gmail.com";
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            bool result = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, r.Code);
            await Send.OkAsync(result);
            return;

        }
        await Send.OkAsync(false);
    }
}
