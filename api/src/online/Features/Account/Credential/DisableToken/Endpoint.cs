using Microsoft.AspNetCore.Identity;
using Online.Common;
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

        string template = """
        <p>Hi {{name}},</p>
        <p>You have requested to disable your TOTP credential. Please use the following OTP code to confirm this action:</p>
        <h2>{{otpCode}}</h2>
        """
        .Replace("{{name}}", user.FirstName)
        .Replace("{{otpCode}}", otpCode);

        string message = EmailHelpers.EmailBody(template);


        await emailService.EnqueueEmailAsync(
            [new(user.Email, user.UserName ?? "")],
            "Your OTP code for disabling credential",
           message,
            isHtml: true,
            cancellationToken: ct
        );
        await Send.NoContentAsync(ct);
    }
}
