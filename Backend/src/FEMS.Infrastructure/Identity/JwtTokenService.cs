using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FEMS.Application.Common.Interfaces;
using FEMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FEMS.Infrastructure.Identity;

/// <summary>
/// JWT access tokens (short expiry) + opaque refresh tokens with rotation (section 19).
/// Refresh tokens are stored server-side only as a SHA-256 hash (RefreshToken.TokenHash).
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public string GenerateAccessToken(User user, IEnumerable<string> roles, Guid? employeeId, Guid? deviceId)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        if (employeeId.HasValue) claims.Add(new Claim("employeeId", employeeId.Value.ToString()));
        if (deviceId.HasValue) claims.Add(new Claim("deviceId", deviceId.Value.ToString()));

        var expiryMinutes = int.Parse(jwtSection["AccessTokenExpiryMinutes"] ?? "15");
        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string Token, DateTimeOffset ExpiresAt) GenerateRefreshToken()
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var days = int.Parse(jwtSection["RefreshTokenExpiryDays"] ?? "7");

        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);
        return (token, DateTimeOffset.UtcNow.AddDays(days));
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
