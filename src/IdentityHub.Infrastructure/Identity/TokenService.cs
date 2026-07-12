using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdentityHub.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityHub.Infrastructure.Identity;

internal sealed class TokenService : ITokenService
{
    internal const string RoleClaimType = "role";
    
    private readonly JwtOptions _jwtOptions;
    private readonly SigningCredentials _signingCredentials;
    private static readonly JsonWebTokenHandler Handler = new();
    
    public TokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtOptions.Secret)),
            SecurityAlgorithms.HmacSha256);
    }

    public string CreateAccessToken(Guid userId, string email, IReadOnlyList<string> roles)
    {
        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, userId.ToString()),
            new (JwtRegisteredClaimNames.Email, email),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        
        claims.AddRange(roles.Select(role => new Claim(RoleClaimType, role)));
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            // Short‑lived access tokens (default 15 min, see JwtOptions)
            // to reduce blast radius if a token is leaked or intercepted.
            // Forces clients to rely on refresh‑token rotation for continued access and enables revoking access.
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes),
            SigningCredentials = _signingCredentials
        };

        return Handler.CreateToken(tokenDescriptor);
    }

    public string GenerateRefreshTokenValue()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var refreshToken = Convert.ToBase64String(randomBytes);

        return refreshToken;
    }
}