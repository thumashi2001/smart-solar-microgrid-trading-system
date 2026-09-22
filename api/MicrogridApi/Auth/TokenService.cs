// =============================================================================
// File: TokenService.cs
// Description: Issues signed JWT access tokens while preserving LoginResponse shape.
// Author: Suwani (Component 4) — shared auth change for Thumashi review
// =============================================================================

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MicrogridApi.Auth;

public class TokenService
{
    private readonly JwtSettings _settings;

    // Constructs the token service with validated JWT settings.
    public TokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    // Creates a signed JWT for the authenticated principal (user or prosumer).
    public string CreateToken(
        string subjectId,
        string role,
        string fullName,
        string? emailOrNic = null)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey) || _settings.SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JwtSettings:SecretKey must be configured with at least 32 characters.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subjectId),
            new(ClaimTypes.NameIdentifier, subjectId),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Name, fullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (!string.IsNullOrWhiteSpace(emailOrNic))
        {
            claims.Add(new Claim("identifier", emailOrNic));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
