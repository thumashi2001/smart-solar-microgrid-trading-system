// =============================================================================
// File: TokenServiceTests.cs
// Description: Ensures JWT issuance requires a strong secret and embeds role claims.
// Author: Suwani (Component 4)
// =============================================================================

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MicrogridApi.Auth;
using Microsoft.Extensions.Options;
using Xunit;

namespace MicrogridApi.Tests;

public class TokenServiceTests
{
    [Fact]
    public void CreateToken_Requires_Configured_Secret()
    {
        var service = new TokenService(Options.Create(new JwtSettings
        {
            SecretKey = "short"
        }));

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateToken("u1", "GridOperator", "Op"));
    }

    [Fact]
    public void CreateToken_Embeds_Role_And_Subject()
    {
        var service = new TokenService(Options.Create(new JwtSettings
        {
            Issuer = "MicrogridApi",
            Audience = "MicrogridClients",
            SecretKey = "LOCAL_DEV_ONLY_CHANGE_ME_32CHARS_MIN_ABCDEF",
            ExpiryMinutes = 60
        }));

        var jwt = service.CreateToken("user-1", "GridOperator", "Operator One", "op@example.com");
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        Assert.Equal("user-1", token.Subject);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "GridOperator");
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Name && c.Value == "Operator One");
    }
}
