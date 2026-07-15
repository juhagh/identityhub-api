using System.Text;
using IdentityHub.Application.Common.Options;
using IdentityHub.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityHub.Infrastructure.Tests;

public sealed class TokenServiceTests
{
    private const string Issuer = "IdentityHub.Tests";
    private const string Audience = "IdentityHub.Client";
    private const string Secret = "this-is-a-long-test-secret-with-more-than-32-bytes";

    private static TokenService CreateSut(
        int accessTokenLifetimeMinutes = 15,
        int refreshTokenLifetimeDays = 7)
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            Secret = Secret
        });

        var authenticationOptions = Options.Create(new AuthenticationOptions
        {
            AccessTokenLifetimeMinutes = accessTokenLifetimeMinutes,
            RefreshTokenLifetimeDays = refreshTokenLifetimeDays
        });

        return new TokenService(
            jwtOptions,
            authenticationOptions);
    }

    [Fact]
    public void CreateAccessToken_Should_Return_Claims_And_Roles()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var email = "user@example.com";
        var roles = new [] {"Admin", "User"};
        
        var token = sut.CreateAccessToken(userId, email, roles);
        var parsedToken = new JsonWebTokenHandler()
            .ReadJsonWebToken(token);
        
        Assert.Equal(userId.ToString(), parsedToken.GetClaim(JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(email, parsedToken.GetClaim(JwtRegisteredClaimNames.Email).Value);
        
        var roleClaims = parsedToken.Claims
            .Where(c => c.Type == TokenService.RoleClaimType)
            .Select(c => c.Value)
            .ToList();

        Assert.Equal(roles.Length, roleClaims.Count);
        Assert.All(roles, role => Assert.Contains(role, roleClaims));
    }
    
    [Fact]
    public async Task CreateAccessToken_ShouldReturnValidToken()
    {
        var sut = CreateSut();
        var token = sut.CreateAccessToken(
            Guid.NewGuid(),
            "user@example.com",
            ["User", "Admin"]);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var result = await new JsonWebTokenHandler()
            .ValidateTokenAsync(token, validationParameters);
        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Fact]
    public void GenerateRefreshTokenValue_ShouldReturnUniqueBase64Encoded64ByteValue()
    {
        var sut = CreateSut();
        
        var rtFirst = sut.GenerateRefreshTokenValue();
        var rtSecond = sut.GenerateRefreshTokenValue();
        
        var decodedBytes = Convert.FromBase64String(rtFirst);
        
        Assert.Equal(64, decodedBytes.Length);
        Assert.NotEqual(rtFirst, rtSecond);
    }

    [Fact]
    public void CreateAccessToken_ShouldSetCustomTokenExpiry()
    {
        var sut = CreateSut(5);
        var userId = Guid.NewGuid();
        var email = "user@example.com";
        var roles = new [] {"Admin", "User"};
        
        var token = sut.CreateAccessToken(userId, email, roles);
        var parsedToken = new JsonWebTokenHandler()
            .ReadJsonWebToken(token);
        
        var expectedExpiry = DateTime.UtcNow.AddMinutes(5);
        Assert.InRange(parsedToken.ValidTo, expectedExpiry.AddSeconds(-30), expectedExpiry.AddSeconds(30));
    }
}