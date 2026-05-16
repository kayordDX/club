using Microsoft.AspNetCore.Identity;
using Online.Entities;
using Online.Models;
using Online.Services;

namespace Online.Features.Account.Credential.DisableToken;

public class Endpoint(UserManager<User> userManager, IEmailService emailService) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/account/credential/disableToken");
        Description(x => x.WithName("AccountCredentialDisableToken"));
        Options(x => x.RequireRateLimiting("OnlyOnePerMinutePerUser"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User) ?? throw new Exception("User not found");
        var otpCode = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        if (user.Email == null)
        {
            throw new Exception("User does not have an email");
        }
        await emailService.EnqueueEmailAsync(
            [new(user.Email, user.UserName ?? "")],
            "Your OTP code for disabling credential",
            $"Your OTP code for disabling credential is: {otpCode}",
            isHtml: false,
            cancellationToken: ct
        );
        await Send.NoContentAsync(ct);
    }
}
