using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Online.Data;
using Online.Entities;

namespace Online.Features.Example;

public class Endpoint : EndpointWithoutRequest<string>
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<User> _userManager;

    public Endpoint(AppDbContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public override void Configure()
    {
        Get("/example");
        Description(x => x.WithName("Example"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Task.Delay(2000, ct);
        string email = "kokjaco2@gmail.com";
        var user = await _userManager.FindByEmailAsync(email);
        string result = "";
        if (user != null)
        {
            result = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        }
        await Send.OkAsync(result);
    }
}
