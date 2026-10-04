using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IdentityHub.API.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("The authenticated principal does not contain a valid subject claim.");
        }

        return userId;
    }

    public static string GetRequiredEmail(this ClaimsPrincipal user)
    {
        var email = user.FindFirstValue(JwtRegisteredClaimNames.Email);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "The authenticated principal does not contain a valid email claim.");
        }

        return email;
    }
}