using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Online.Common.Auth;
using Online.Common.Config;

namespace Online.Common.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection ConfigureAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddGoogle(options =>
        {
            options.ClientId = configuration["Authentication:Google:ClientId"] ?? throw new ArgumentException("Google ClientId is not configured");
            options.ClientSecret = configuration["Authentication:Google:ClientSecret"] ?? throw new ArgumentException("Google ClientSecret is not configured");
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            // Added below to get profile info like picture
            options.Scope.Add("profile");
            options.ClaimActions.MapJsonKey("picture", "picture", "url"); // Map the 'picture' claim
        })
        .AddJwtBearer(options =>
        {
            var jwtOptions = configuration.GetSection(JwtOptions.JwtOptionsKey)
                .Get<JwtOptions>() ?? throw new ArgumentException(nameof(JwtOptions));

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies["ACCESS_TOKEN"];
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(Constants.Policy.Role, b => b.Requirements.Add(new RoleTypeRequirement(Constants.Policy.Role)))
            .AddPolicy(Constants.Policy.OutletRole, b => b.Requirements.Add(new OutletRoleTypeRequirement(Constants.Policy.OutletRole)));

        return services;
    }
}
