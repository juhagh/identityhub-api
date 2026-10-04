using System.Text;
using IdentityHub.Application.Common.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityHub.Infrastructure.Identity;

internal sealed class ConfigureJwtBearerOptions(
    IOptions<JwtOptions> jwtOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
            return;
        
        var jwt = jwtOptions.Value;

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new()
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            

            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateLifetime = true,
            // Prevent algorithm-confusion
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            // Require exact token lifetime validation without the default five-minute tolerance.
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = JwtClaimTypes.Role,
            NameClaimType = JwtRegisteredClaimNames.Sub
        };

    }

    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);

}