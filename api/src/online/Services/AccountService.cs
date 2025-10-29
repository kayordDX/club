using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Online.Common.Config;
using Online.Data;
using Online.Entities;
using Online.Models;
using Wangkanai.Detection.Services;

namespace Online.Services;

public class AccountService
{
    private readonly AuthTokenProcessor _authTokenProcessor;
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;
    private readonly IDetectionService _detectionService;
    private readonly CurrentUserService _currentUserService;

    public AccountService(UserManager<User> userManager, AuthTokenProcessor authTokenProcessor, AppDbContext dbContext, IOptions<JwtOptions> jwtOptions, IDetectionService detectionService, CurrentUserService currentUserService)
    {
        _userManager = userManager;
        _authTokenProcessor = authTokenProcessor;
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
        _detectionService = detectionService;
        _currentUserService = currentUserService;
    }

    public async Task RegisterAsync(UserRegisterRequest registerRequest)
    {
        var userExists = await _userManager.FindByEmailAsync(registerRequest.Email) != null;

        if (userExists)
        {
            throw new Exception($"User already exists {registerRequest.Email}");
        }

        var user = new User
        {
            // Id = registerRequest.Email,
            Email = registerRequest.Email,
            UserName = registerRequest.Email,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName
        };

        var result = await _userManager.CreateAsync(user, registerRequest.Password);

        if (!result.Succeeded)
        {
            throw new Exception($"Registration failed with following errors: {string.Join(Environment.NewLine, result.Errors.Select(x => x.Description))}");
        }
    }

    private async Task LoginShared(User user, UserRefreshToken? tokenValue)
    {
        var (jwtToken, expirationDateInUtc) = _authTokenProcessor.GenerateJwtToken(user);
        var refreshTokenValue = _authTokenProcessor.GenerateRefreshToken();

        var refreshTokenExpirationDateInUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpirationTimeInMinutes);

        string browser = _detectionService.Browser.Name.ToString();
        string browserVersion = _detectionService.Browser.Version.ToString();
        string device = _detectionService.Device.Type.ToString();
        string platform = _detectionService.Platform.Name.ToString();
        string platformProcessor = _detectionService.Platform.Processor.ToString();

        if (tokenValue != null)
        {
            tokenValue.Token = refreshTokenValue;
            tokenValue.ExpiresAtUtc = refreshTokenExpirationDateInUtc;
            tokenValue.Browser = browser;
            tokenValue.BrowserVersion = browserVersion;
            tokenValue.Device = device;
            tokenValue.Platform = platform;
            tokenValue.Processor = platformProcessor;
        }
        else
        {
            tokenValue = new UserRefreshToken
            {
                Token = refreshTokenValue,
                UserId = user.Id,
                ExpiresAtUtc = refreshTokenExpirationDateInUtc,
                Browser = browser,
                BrowserVersion = browserVersion,
                Device = device,
                Platform = platform,
                Processor = platformProcessor
            };
            await _dbContext.UserRefreshToken.AddAsync(tokenValue);
        }
        await _dbContext.SaveChangesAsync();

        _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", jwtToken, expirationDateInUtc);
        _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", refreshTokenValue, refreshTokenExpirationDateInUtc);
        _authTokenProcessor.WriteAuthTokenAsClientCookie("HAS_TOKEN", expirationDateInUtc);
    }

    public void Logout()
    {
        _authTokenProcessor.DeleteAuthCookie("ACCESS_TOKEN");
        _authTokenProcessor.DeleteAuthCookie("REFRESH_TOKEN");
        _authTokenProcessor.DeleteAuthCookie("HAS_TOKEN");
    }

    public async Task LoginAsync(LoginRequest loginRequest)
    {
        var user = await _userManager.FindByEmailAsync(loginRequest.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, loginRequest.Password))
        {
            throw new Exception($"Login Failed exception for {loginRequest.Email}");
        }

        await LoginShared(user, null);
    }

    public async Task RefreshTokenAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new Exception("Refresh token is missing");
        }

        var tokenValue = await _dbContext.UserRefreshToken
            .Where(x => x.Token == refreshToken)
            .Include(x => x.User)
            .FirstOrDefaultAsync(ct);

        if (tokenValue == null || tokenValue.User == null)
        {
            throw new Exception("Unable to retrieve user for refresh token");
        }

        if (tokenValue.ExpiresAtUtc < DateTime.UtcNow)
        {
            throw new Exception("Refresh token is expired");
        }

        await LoginShared(tokenValue.User, tokenValue);
    }

    // TODO: Validate This
    public async Task RevokeAll()
    {
        var userId = _currentUserService.GetId();
        var refreshTokens = _dbContext.UserRefreshToken.Where(x => x.UserId == userId);
        _dbContext.UserRefreshToken.RemoveRange(refreshTokens);
        await _dbContext.SaveChangesAsync();
    }

    // TODO: Add Revoke to Revoke Specific Device

    public async Task LoginWithGoogleAsync(ClaimsPrincipal? claimsPrincipal)
    {
        if (claimsPrincipal == null)
        {
            throw new Exception($"External login provider: Google error occurred: ClaimsPrincipal is null");
        }

        var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
        var picture = claimsPrincipal.FindFirstValue("picture");

        if (email == null)
        {
            throw new Exception($"External login provider: Google error occurred: Email is null");
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            var newUser = new User
            {
                UserName = email,
                Email = email,
                FirstName = claimsPrincipal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
                LastName = claimsPrincipal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
                EmailConfirmed = true,
                Picture = picture
            };

            var result = await _userManager.CreateAsync(newUser);

            if (!result.Succeeded)
            {
                throw new Exception($"External login provider: Google error occurred: Unable to create user: {string.Join(", ",
                    result.Errors.Select(x => x.Description))}");
            }

            user = newUser;

            var info = new UserLoginInfo("Google",
                claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                "Google");

            var loginResult = await _userManager.AddLoginAsync(user, info);

            if (!loginResult.Succeeded)
            {
                throw new Exception($"External login provider: Google error occurred: Unable to login user {string.Join(", ",
                    loginResult.Errors.Select(x => x.Description))}");
            }
        }
        else
        {
            // Update picture if changed
            if (user.Picture != picture)
            {
                user.Picture = picture;
                await _userManager.UpdateAsync(user);
            }
        }

        await LoginShared(user, null);
    }
}