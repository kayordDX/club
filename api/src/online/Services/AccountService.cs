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

        var user = User.Create(registerRequest.Email, registerRequest.FirstName, registerRequest.LastName);
        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, registerRequest.Password);

        var result = await _userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            throw new Exception($"Registration failed with following errors: {string.Join(Environment.NewLine, result.Errors)}");
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
}