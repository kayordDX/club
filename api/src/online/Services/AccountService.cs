using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Online.Entities;
using Online.Features.Account.Register;

namespace Online.Services;

public class AccountService
{
    private readonly AuthTokenProcessor _authTokenProcessor;
    private readonly UserManager<User> _userManager;
    private readonly UserRepository _userRepository;

    public AccountService(UserManager<User> userManager, AuthTokenProcessor authTokenProcessor, UserRepository userRepository)
    {
        _userManager = userManager;
        _authTokenProcessor = authTokenProcessor;
        _userRepository = userRepository;
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

    public async Task LoginAsync(LoginRequest loginRequest)
    {
        var user = await _userManager.FindByEmailAsync(loginRequest.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, loginRequest.Password))
        {
            throw new Exception($"Login Failed exception for {loginRequest.Email}");
        }

        var (jwtToken, expirationDateInUtc) = _authTokenProcessor.GenerateJwtToken(user);
        var refreshTokenValue = _authTokenProcessor.GenerateRefreshToken();

        var refreshTokenExpirationDateInUtc = DateTime.UtcNow.AddDays(7);

        user.RefreshToken = refreshTokenValue;
        user.RefreshTokenExpiresAtUtc = refreshTokenExpirationDateInUtc;

        await _userManager.UpdateAsync(user);

        _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", jwtToken, expirationDateInUtc);
        _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", user.RefreshToken, refreshTokenExpirationDateInUtc);
    }

    public async Task RefreshTokenAsync(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new Exception("Refresh token is missing");
        }

        var user = await _userRepository.GetUserByRefreshTokenAsync(refreshToken);
        if (user == null)
        {
            throw new Exception("Unable to retrieve user for refresh token");
        }

        if (user.RefreshTokenExpiresAtUtc < DateTime.UtcNow)
        {
            throw new Exception("Refresh token is expired");
        }

        var (jwtToken, expirationDateInUtc) = _authTokenProcessor.GenerateJwtToken(user);
        var refreshTokenValue = _authTokenProcessor.GenerateRefreshToken();

        var refreshTokenExpirationDateInUtc = DateTime.UtcNow.AddDays(7);

        user.RefreshToken = refreshTokenValue;
        user.RefreshTokenExpiresAtUtc = refreshTokenExpirationDateInUtc;

        await _userManager.UpdateAsync(user);

        _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", jwtToken, expirationDateInUtc);
        _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", user.RefreshToken, refreshTokenExpirationDateInUtc);
    }

    public async Task LoginWithGoogleAsync(ClaimsPrincipal? claimsPrincipal)
    {
        if (claimsPrincipal == null)
        {
            throw new Exception($"External login provider: Google error occurred: ClaimsPrincipal is null");
        }

        var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email);

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
                EmailConfirmed = true
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

        var (jwtToken, expirationDateInUtc) = _authTokenProcessor.GenerateJwtToken(user);
        var refreshTokenValue = _authTokenProcessor.GenerateRefreshToken();

        var refreshTokenExpirationDateInUtc = DateTime.UtcNow.AddDays(7);

        user.RefreshToken = refreshTokenValue;
        user.RefreshTokenExpiresAtUtc = refreshTokenExpirationDateInUtc;

        await _userManager.UpdateAsync(user);

        _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", jwtToken, expirationDateInUtc);
        _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", user.RefreshToken, refreshTokenExpirationDateInUtc);
    }
}