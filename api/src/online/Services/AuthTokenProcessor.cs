using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Online.Common.Config;
using Online.Data;
using Online.Entities;

namespace Online.Services;

public class AuthTokenProcessor(IOptions<JwtOptions> jwtOptions, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IWebHostEnvironment _environment = environment;

    public async Task<(string jwtToken, DateTime expiresAtUtc)> GenerateJwtTokenAsync(User user)
    {
        var signinKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Name, user.ToString()),
            new(JwtRegisteredClaimNames.Picture, user.Picture ?? "")
        };

        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationTimeInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwtToken, expires);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public void WriteAuthTokenAsHttpOnlyCookie(string cookieName, string token, DateTime expiration)
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Expires = expiration,
            IsEssential = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict
        });
    }

    public void WriteAuthTokenAsClientCookie(string cookieName, string value, DateTime expiration)
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieName, value, new CookieOptions
        {
            HttpOnly = false,
            Expires = expiration,
            IsEssential = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict
        });
    }

    public void DeleteAuthCookie(string cookieName)
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(cookieName);
    }
}
