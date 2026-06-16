using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Identity.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace UnitTests.Identity;

public sealed class JwtTokenServiceTests
{
    private static JwtTokenService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSecretKeyForUnitTesting256Bits!!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
            })
            .Build();

        return new JwtTokenService(config);
    }

    [Fact]
    public void GenerateAccessToken_ProducesValidJwt()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var token = service.GenerateAccessToken(userId, "test@example.com", "Patient", "Test User");

        Assert.NotEmpty(token);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "TestAudience");
    }

    [Fact]
    public void GenerateAccessToken_ContainsRoleClaim()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var token = service.GenerateAccessToken(userId, "test@example.com", "Admin", "Admin User");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("Admin", roleClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_ContainsSubjectClaim()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var token = service.GenerateAccessToken(userId, "test@example.com", "Patient", "Test User");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(sub);
        Assert.Equal(userId.ToString(), sub.Value);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresIn15Minutes()
    {
        var service = CreateService();
        var before = DateTime.UtcNow;

        var token = service.GenerateAccessToken(Guid.NewGuid(), "t@e.com", "Patient", "T");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var expiry = jwt.ValidTo;
        Assert.True(expiry > before.AddMinutes(14));
        Assert.True(expiry < before.AddMinutes(16));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueTokens()
    {
        var service = CreateService();

        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void Constructor_MissingKey_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config));
    }
}
